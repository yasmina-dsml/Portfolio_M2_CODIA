#ifndef RUNTIME_H
#define RUNTIME_H

#include <stddef.h>
#include <sys/types.h>

/* Types du runtime : entier, flottant, chaîne. */
typedef enum {
    RT_INT = 0,
    RT_FLOAT,
    RT_STRING
} RtType;

/* Table générique alimentée par les programmes générés. */
typedef struct {
    size_t ncols;
    size_t rows;
    size_t capacity;
    RtType *types;     /* taille ncols */
    void   **columns;  /* columns[c] pointe vers un tableau du type adéquat */
    char   **col_names;/* optionnel */
} RtTable;

typedef struct {
    RtType type;
    void *data;
    size_t stride;   /* taille en octets entre deux éléments successifs */
    size_t length;   /* nombre d'éléments adressables */
    int owns_data;   /* 1 si data doit être libéré */
} RtColumn;

/* I/O CSV avec validation du header et des types */
int  rt_load_csv(const char *path, const RtType *types, const char *const *names, size_t ncols, RtTable *out);
void rt_free_table(RtTable *table);

/* Statistiques numériques */
int    rt_column_is_numeric(const RtTable *table, size_t col);
double rt_mean   (const RtTable *table, size_t col, int *ok);
double rt_median (const RtTable *table, size_t col, int *ok);
double rt_stddev (const RtTable *table, size_t col, int *ok);
double rt_min_num(const RtTable *table, size_t col, int *ok);
double rt_max_num(const RtTable *table, size_t col, int *ok);

/* Histogramme (string) : renvoie le nb de valeurs uniques, remplit les tableaux si fournis. */
size_t rt_histogram_string(const RtTable *table, size_t col, char ***values_out, size_t **counts_out);

/* Helpers de manipulation de tables */
int  rt_table_clone_structure(const RtTable *src, RtTable *dest, size_t reserve_rows);
ssize_t rt_table_append_row(RtTable *table);
int  rt_table_add_column(RtTable *table, RtType type);
int  rt_table_copy_row(RtTable *dest, ssize_t dest_row, const RtTable *src, size_t src_row);
int  rt_table_copy_row_into(RtTable *dest, ssize_t dest_row, const RtTable *src, size_t src_row, size_t dest_col_offset);
int  rt_table_init_with_types(const RtType *types, size_t ncols, size_t reserve_rows, RtTable *out);

/* Vues/copies de colonnes */
RtColumn rt_column_view(const RtTable *table, size_t col);
RtColumn rt_column_copy(const RtTable *table, size_t col);
void rt_column_release(RtColumn *col);

#endif
