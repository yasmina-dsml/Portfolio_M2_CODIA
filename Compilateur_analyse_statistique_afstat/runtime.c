#include "runtime.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>

#define RT_INIT_CAP 128
#define RT_LINE_BUF 4096

static size_t element_size(RtType t) {
    switch (t) {
    case RT_INT:   return sizeof(long long);
    case RT_FLOAT: return sizeof(double);
    case RT_STRING:return sizeof(char *);
    }
    return sizeof(void *);
}

static char *rt_strdup(const char *src) {
    size_t len = strlen(src) + 1;
    char *dup = malloc(len);
    if (dup) memcpy(dup, src, len);
    return dup;
}

static int ensure_capacity(RtTable *tab, size_t new_rows) {
    if (new_rows <= tab->capacity) return 0;
    size_t new_cap = tab->capacity ? tab->capacity * 2 : RT_INIT_CAP;
    while (new_cap < new_rows) new_cap *= 2;
    for (size_t c = 0; c < tab->ncols; ++c) {
        size_t es = element_size(tab->types[c]);
        void *p = realloc(tab->columns[c], es * new_cap);
        if (!p) return -1;
        tab->columns[c] = p;
    }
    tab->capacity = new_cap;
    return 0;
}

RtColumn rt_column_view(const RtTable *table, size_t col) {
    RtColumn view;
    view.data = NULL;
    view.type = RT_STRING;
    view.stride = 0;
    view.length = 0;
    view.owns_data = 0;
    if (!table || col >= table->ncols) return view;
    view.type = table->types[col];
    view.data = table->columns[col];
    view.stride = element_size(view.type);
    view.length = table->rows;
    return view;
}

RtColumn rt_column_copy(const RtTable *table, size_t col) {
    RtColumn copy = rt_column_view(table, col);
    if (!copy.data || copy.length == 0) {
        copy.owns_data = 0;
        return copy;
    }
    size_t es = element_size(copy.type);
    if (copy.type == RT_STRING) {
        char **src = (char **)table->columns[col];
        char **dup = (char **)calloc(copy.length, sizeof(char *));
        if (!dup) {
            copy.data = NULL;
            copy.length = 0;
            copy.owns_data = 0;
            return copy;
        }
        for (size_t i = 0; i < copy.length; ++i) {
            dup[i] = src[i] ? rt_strdup(src[i]) : NULL;
        }
        copy.data = dup;
    } else {
        void *dup = malloc(es * copy.length);
        if (!dup) {
            copy.data = NULL;
            copy.length = 0;
            copy.owns_data = 0;
            return copy;
        }
        memcpy(dup, table->columns[col], es * copy.length);
        copy.data = dup;
    }
    copy.owns_data = 1;
    copy.stride = es;
    return copy;
}

void rt_column_release(RtColumn *col) {
    if (!col || !col->owns_data || !col->data) return;
    if (col->type == RT_STRING) {
        char **data = (char **)col->data;
        for (size_t i = 0; i < col->length; ++i) {
            free(data[i]);
        }
    }
    free(col->data);
    col->data = NULL;
    col->length = 0;
    col->owns_data = 0;
}

int rt_table_add_column(RtTable *table, RtType type) {
    if (!table) return -1;
    size_t new_ncols = table->ncols + 1;
    RtType *types = (RtType *)realloc(table->types, new_ncols * sizeof(RtType));
    if (!types) return -1;
    void **cols = (void **)realloc(table->columns, new_ncols * sizeof(void *));
    if (!cols) return -1;
    char **names = NULL;
    if (table->col_names) {
        names = (char **)realloc(table->col_names, new_ncols * sizeof(char *));
        if (!names) return -1;
        names[new_ncols - 1] = NULL;
        table->col_names = names;
    }
    table->types = types;
    table->columns = cols;
    size_t es = element_size(type);
    if (table->capacity == 0) {
        size_t reserve = table->rows ? table->rows : RT_INIT_CAP;
        if (ensure_capacity(table, reserve) != 0) return -1;
    }
    if (type == RT_STRING) {
        table->columns[new_ncols - 1] = calloc(table->capacity, sizeof(char *));
    } else {
        table->columns[new_ncols - 1] = calloc(table->capacity, es);
    }
    if (!table->columns[new_ncols - 1]) return -1;
    table->types[new_ncols - 1] = type;
    table->ncols = new_ncols;
    return (int)(new_ncols - 1);
}

int rt_table_clone_structure(const RtTable *src, RtTable *dest, size_t reserve_rows) {
    if (!src || !dest) return -1;
    memset(dest, 0, sizeof(*dest));
    dest->ncols = src->ncols;
    if (dest->ncols) {
        dest->types = (RtType *)malloc(dest->ncols * sizeof(RtType));
        dest->columns = (void **)calloc(dest->ncols, sizeof(void *));
        if (!dest->types || !dest->columns) {
            rt_free_table(dest);
            return -1;
        }
        memcpy(dest->types, src->types, dest->ncols * sizeof(RtType));
    }
    if (reserve_rows == 0) reserve_rows = src->rows;
    if (reserve_rows > 0) {
        if (ensure_capacity(dest, reserve_rows) != 0) {
            rt_free_table(dest);
            return -1;
        }
    }
    dest->rows = 0;
    return 0;
}

ssize_t rt_table_append_row(RtTable *table) {
    if (!table) return -1;
    if (ensure_capacity(table, table->rows + 1) != 0) return -1;
    size_t idx = table->rows++;
    for (size_t c = 0; c < table->ncols; ++c) {
        if (table->types[c] == RT_STRING) {
            ((char **)table->columns[c])[idx] = NULL;
        }
    }
    return (ssize_t)idx;
}

int rt_table_copy_row(RtTable *dest, ssize_t dest_row, const RtTable *src, size_t src_row) {
    if (!dest || !src) return -1;
    if (dest->ncols != src->ncols) return -1;
    if (dest_row < 0 || (size_t)dest_row >= dest->rows) return -1;
    if (src_row >= src->rows) return -1;
    for (size_t c = 0; c < dest->ncols; ++c) {
        if (dest->types[c] != src->types[c]) {
            return -1;
        }
        switch (dest->types[c]) {
        case RT_INT:
            ((long long *)dest->columns[c])[dest_row] = ((long long *)src->columns[c])[src_row];
            break;
        case RT_FLOAT:
            ((double *)dest->columns[c])[dest_row] = ((double *)src->columns[c])[src_row];
            break;
        case RT_STRING:
            {
                char **dst = (char **)dest->columns[c];
                char **source = (char **)src->columns[c];
                if (dst[dest_row]) {
                    free(dst[dest_row]);
                }
                dst[dest_row] = source[src_row] ? rt_strdup(source[src_row]) : NULL;
            }
            break;
        }
    }
    return 0;
}

int rt_table_copy_row_into(RtTable *dest, ssize_t dest_row, const RtTable *src, size_t src_row, size_t dest_col_offset) {
    if (!dest || !src) return -1;
    if (dest_row < 0 || (size_t)dest_row >= dest->rows) return -1;
    if (src_row >= src->rows) return -1;
    if (dest_col_offset + src->ncols > dest->ncols) return -1;
    for (size_t c = 0; c < src->ncols; ++c) {
        size_t dc = dest_col_offset + c;
        if (dest->types[dc] != src->types[c]) {
            return -1;
        }
        switch (dest->types[dc]) {
        case RT_INT:
            ((long long *)dest->columns[dc])[dest_row] = ((long long *)src->columns[c])[src_row];
            break;
        case RT_FLOAT:
            ((double *)dest->columns[dc])[dest_row] = ((double *)src->columns[c])[src_row];
            break;
        case RT_STRING:
            {
                char **dst = (char **)dest->columns[dc];
                char **source = (char **)src->columns[c];
                if (dst[dest_row]) {
                    free(dst[dest_row]);
                }
                dst[dest_row] = source[src_row] ? rt_strdup(source[src_row]) : NULL;
            }
            break;
        }
    }
    return 0;
}

int rt_table_init_with_types(const RtType *types, size_t ncols, size_t reserve_rows, RtTable *out) {
    if (!types || !out || ncols == 0) return -1;
    memset(out, 0, sizeof(*out));
    out->ncols = ncols;
    out->types = (RtType *)malloc(ncols * sizeof(RtType));
    out->columns = (void **)calloc(ncols, sizeof(void *));
    if (!out->types || !out->columns) {
        rt_free_table(out);
        return -1;
    }
    memcpy(out->types, types, ncols * sizeof(RtType));
    if (reserve_rows == 0) reserve_rows = RT_INIT_CAP;
    out->capacity = 0;
    if (ensure_capacity(out, reserve_rows) != 0) {
        rt_free_table(out);
        return -1;
    }
    out->rows = 0;
    return 0;
}

static int parse_csv_line(char *line, char **fields, size_t max_fields) {
    size_t nf = 0;
    char *tok = strtok(line, ",");
    while (tok && nf < max_fields) {
        fields[nf++] = tok;
        tok = strtok(NULL, ",");
    }
    return (int)nf;
}

static int parse_int_field(const char *text, long long *out) {
    if (!text) return -1;
    char *end = NULL;
    long long v = strtoll(text, &end, 10);
    if (text == end || (end && *end != '\0')) return -1;
    if (out) *out = v;
    return 0;
}

static int parse_float_field(const char *text, double *out) {
    if (!text) return -1;
    char *end = NULL;
    double v = strtod(text, &end);
    if (text == end || (end && *end != '\0')) return -1;
    if (out) *out = v;
    return 0;
}

int rt_load_csv(const char *path, const RtType *types, const char *const *names, size_t ncols, RtTable *out) {
    memset(out, 0, sizeof(*out));
    out->ncols = ncols;
    out->types = malloc(ncols * sizeof(RtType));
    out->columns = calloc(ncols, sizeof(void *));
    if (!out->types || !out->columns) return -1;
    memcpy(out->types, types, ncols * sizeof(RtType));

    FILE *f = fopen(path, "r");
    if (!f) {
        fprintf(stderr, "Unable to open CSV '%s'\n", path);
        return -1;
    }

    char line[RT_LINE_BUF];

    /* Header */
    if (!fgets(line, sizeof(line), f)) {
        fclose(f);
        fprintf(stderr, "Empty CSV '%s'\n", path);
        return -1;
    }
    for (char *p = line; *p; ++p) if (*p == '\r' || *p == '\n') *p = '\0';
    char *header_fields[512];
    int header_cols = parse_csv_line(line, header_fields, 512);
    if (header_cols < 0) {
        fclose(f);
        fprintf(stderr, "CSV '%s' header parse error\n", path);
        return -1;
    }

    int *col_map = NULL;
    if (names) {
        col_map = (int *)malloc(ncols * sizeof(int));
        if (!col_map) {
            fclose(f);
            return -1;
        }
        int missing = 0;
        for (size_t i = 0; i < ncols; ++i) {
            col_map[i] = -1;
            for (int h = 0; h < header_cols; ++h) {
                if (strcmp(header_fields[h], names[i]) == 0) {
                    col_map[i] = h;
                    break;
                }
            }
            if (col_map[i] < 0) {
                fprintf(stderr, "CSV '%s' missing column '%s'\n", path, names[i]);
                missing = 1;
            }
        }
        if (missing) {
            free(col_map);
            fclose(f);
            return -1;
        }
    }

    /* Rows */
    size_t row_num = 1; /* header */
    while (fgets(line, sizeof(line), f)) {
        ++row_num;
        for (char *p = line; *p; ++p) if (*p == '\r' || *p == '\n') *p = '\0';
        char *fields[256];
        int nf = parse_csv_line(line, fields, 256);
        if (nf < 0) {
            fprintf(stderr, "CSV '%s' line %zu parse error\n", path, row_num);
            free(col_map);
            fclose(f);
            return -1;
        }
        int max_index = nf - 1;
        if (col_map) {
            for (size_t i = 0; i < ncols; ++i) {
                if (col_map[i] > max_index) {
                    fprintf(stderr, "CSV '%s' line %zu has %d field(s), column '%s' is missing\n", path, row_num, nf, names[i]);
                    free(col_map);
                    fclose(f);
                    return -1;
                }
            }
        } else if ((size_t)nf < ncols) {
            fprintf(stderr, "CSV '%s' line %zu has %d field(s), expected %zu\n", path, row_num, nf, ncols);
            free(col_map);
            fclose(f);
            return -1;
        }

        if (ensure_capacity(out, out->rows + 1) != 0) { free(col_map); fclose(f); return -1; }
        size_t r = out->rows;

        for (size_t c = 0; c < ncols; ++c) {
            size_t field_index = col_map ? (size_t)col_map[c] : c;
            switch (out->types[c]) {
            case RT_INT: {
                long long v = 0;
                if (parse_int_field(fields[field_index], &v) != 0) {
                    fprintf(stderr, "Type mismatch in '%s' line %zu column '%s': expected integer, got '%s'\n",
                            path, row_num, names ? names[c] : "int", fields[field_index]);
                    free(col_map);
                    fclose(f);
                    return -1;
                }
                ((long long *)out->columns[c])[r] = v;
                break;
            }
            case RT_FLOAT: {
                double v = 0.0;
                if (parse_float_field(fields[field_index], &v) != 0) {
                    fprintf(stderr, "Type mismatch in '%s' line %zu column '%s': expected float, got '%s'\n",
                            path, row_num, names ? names[c] : "float", fields[field_index]);
                    free(col_map);
                    fclose(f);
                    return -1;
                }
                ((double *)out->columns[c])[r] = v;
                break;
            }
            case RT_STRING: {
                char **col = (char **)out->columns[c];
                char *dup = rt_strdup(fields[field_index] ? fields[field_index] : "");
                if (!dup) { free(col_map); fclose(f); return -1; }
                col[r] = dup;
                break;
            }
            }
        }

        out->rows++;
    }

    free(col_map);
    fclose(f);
    return 0;
}

void rt_free_table(RtTable *t) {
    if (!t) return;
    for (size_t c = 0; c < t->ncols; ++c) {
        if (t->types[c] == RT_STRING && t->columns[c]) {
            char **col = (char **)t->columns[c];
            for (size_t r = 0; r < t->rows; ++r) free(col[r]);
        }
        free(t->columns[c]);
    }
    free(t->columns);
    free(t->types);
    free(t->col_names);
    memset(t, 0, sizeof(*t));
}

int rt_column_is_numeric(const RtTable *t, size_t col) {
    return t && col < t->ncols && (t->types[col] == RT_INT || t->types[col] == RT_FLOAT);
}

static double *column_as_double(const RtTable *t, size_t col) {
    if (!rt_column_is_numeric(t, col)) return NULL;
    double *buf = malloc(t->rows * sizeof(double));
    if (!buf) return NULL;
    if (t->types[col] == RT_INT) {
        long long *src = (long long *)t->columns[col];
        for (size_t i = 0; i < t->rows; ++i) buf[i] = (double)src[i];
    } else {
        double *src = (double *)t->columns[col];
        memcpy(buf, src, t->rows * sizeof(double));
    }
    return buf;
}

double rt_mean(const RtTable *t, size_t col, int *ok) {
    if (ok) *ok = 0;
    if (!rt_column_is_numeric(t, col) || t->rows == 0) return 0.0;
    double *vals = column_as_double(t, col);
    if (!vals) return 0.0;
    double sum = 0.0;
    for (size_t i = 0; i < t->rows; ++i) sum += vals[i];
    free(vals);
    if (ok) *ok = 1;
    return sum / (double)t->rows;
}

static int cmp_double(const void *a, const void *b) {
    double da = *(const double *)a, db = *(const double *)b;
    return (da > db) - (da < db);
}

double rt_median(const RtTable *t, size_t col, int *ok) {
    if (ok) *ok = 0;
    if (!rt_column_is_numeric(t, col) || t->rows == 0) return 0.0;
    double *vals = column_as_double(t, col);
    if (!vals) return 0.0;
    qsort(vals, t->rows, sizeof(double), cmp_double);
    double med = (t->rows % 2 == 0)
        ? (vals[t->rows/2 - 1] + vals[t->rows/2]) / 2.0
        : vals[t->rows/2];
    free(vals);
    if (ok) *ok = 1;
    return med;
}

double rt_min_num(const RtTable *t, size_t col, int *ok) {
    if (ok) *ok = 0;
    if (!rt_column_is_numeric(t, col) || t->rows == 0) return 0.0;
    double *vals = column_as_double(t, col);
    if (!vals) return 0.0;
    double min = vals[0];
    for (size_t i = 1; i < t->rows; ++i) {
        if (vals[i] < min) min = vals[i];
    }
    free(vals);
    if (ok) *ok = 1;
    return min;
}

double rt_max_num(const RtTable *t, size_t col, int *ok) {
    if (ok) *ok = 0;
    if (!rt_column_is_numeric(t, col) || t->rows == 0) return 0.0;
    double *vals = column_as_double(t, col);
    if (!vals) return 0.0;
    double max = vals[0];
    for (size_t i = 1; i < t->rows; ++i) if (vals[i] > max) max = vals[i];
    free(vals);
    if (ok) *ok = 1;
    return max;
}

double rt_stddev(const RtTable *t, size_t col, int *ok) {
    if (ok) *ok = 0;
    if (!rt_column_is_numeric(t, col) || t->rows < 2) return 0.0;
    double mean = rt_mean(t, col, NULL);
    double *vals = column_as_double(t, col);
    if (!vals) return 0.0;
    double acc = 0.0;
    for (size_t i = 0; i < t->rows; ++i) {
        double d = vals[i] - mean;
        acc += d * d;
    }
    free(vals);
    if (ok) *ok = 1;
    return sqrt(acc / (double)(t->rows - 1));
}

size_t rt_histogram_string(const RtTable *t, size_t col, char ***values_out, size_t **counts_out) {
    if (!t || col >= t->ncols || t->types[col] != RT_STRING) return 0;
    char **src = (char **)t->columns[col];
    size_t uniq_count = 0, uniq_cap = 16;
    char **values = malloc(uniq_cap * sizeof(char *));
    size_t *counts = malloc(uniq_cap * sizeof(size_t));
    if (!values || !counts) { free(values); free(counts); return 0; }

    for (size_t i = 0; i < t->rows; ++i) {
        size_t j;
        for (j = 0; j < uniq_count; ++j) {
            if (strcmp(src[i], values[j]) == 0) {
                counts[j]++;
                break;
            }
        }
        if (j == uniq_count) {
            if (uniq_count == uniq_cap) {
                uniq_cap *= 2;
                values = realloc(values, uniq_cap * sizeof(char *));
                counts = realloc(counts, uniq_cap * sizeof(size_t));
                if (!values || !counts) { free(values); free(counts); return 0; }
            }
            values[uniq_count] = rt_strdup(src[i]);
            counts[uniq_count] = 1;
            uniq_count++;
        }
    }

    if (values_out) *values_out = values; else { for (size_t k=0;k<uniq_count;++k) free(values[k]); free(values); }
    if (counts_out) *counts_out = counts; else free(counts);
    return uniq_count;
}
