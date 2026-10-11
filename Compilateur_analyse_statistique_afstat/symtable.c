#include "symtable.h"

#include <stdio.h>
#include <string.h>
#include <stdlib.h>

static char *sym_strdup(const char *s) {
    if (!s) {
        return NULL;
    }
    size_t len = strlen(s);
    char *copy = (char *)malloc(len + 1);
    if (!copy) {
        return NULL;
    }
    memcpy(copy, s, len + 1);
    return copy;
}

//initialise la table des symboles
void symtable_init(SymbolTable *table) {
    if (!table) {
        return;
    }
    table->last_index = -1;
}

//cherche un symbole dans la table des symboles, retourne son index ou -1 si non trouvé
int symtable_find(const SymbolTable *table, const char *symbol) {
    if (!table || !symbol) {
        return -1;
    }
    for (int i = 0; i <= table->last_index; ++i) {
        if (strcmp(table->entries[i], symbol) == 0) {
            return i;
        }
    }
    return -1;
}

//ajoute un symbole à la table des symboles, retourne son index ou -1 en cas d'erreur
int symtable_add(SymbolTable *table, const char *symbol) {
    if (!table || !symbol) {
        return -1;
    }
    if (table->last_index + 1 >= SYMTABLE_MAX_SYMBOLS) {
        fprintf(stderr, "symbol table overflow\n");
        return -1;
    }

    int index = symtable_find(table, symbol);
    if (index >= 0) {
        return index;
    }

    ++table->last_index;
    strncpy(table->entries[table->last_index], symbol,
            sizeof(table->entries[table->last_index]) - 1);
    table->entries[table->last_index][sizeof(table->entries[table->last_index]) - 1] = '\0';
    return table->last_index;
}

void symtable_dump(const SymbolTable *table) {
    if (!table) {
        return;
    }

    printf("\n\t\t SYMBOL TABLE (%d symbol(s))\n", table->last_index + 1);
    printf("\t-------------------------------------------------\n");
    printf("\t|     index     |\t\t\tsymbol\t\t\t|\n");
    printf("\t-------------------------------------------------\n");

    for (int i = 0; i <= table->last_index; ++i) {
        const char *symbol = table->entries[i];
        size_t len = strlen(symbol);
        if (len < 8) {
            printf("\t|\t%d\t|\t%s\t\t\t|\n", i, symbol);
        } else if (len < 16) {
            printf("\t|\t%d\t|\t%s\t\t|\n", i, symbol);
        } else {
            printf("\t|\t%d\t|\t%s\t|\n", i, symbol);
        }
    }

    printf("\t-------------------------------------------------\n");
}

void symtable_destroy(SymbolTable *table) {
    (void)table;
}
//initialise une source en remplissant son nom et son chemin
void source_init(Source *src, const char *name, const char *path) {
    if (!src) {
        return;
    }
    strncpy(src->source_name, name, NAME_MAX_LENGTH);
    src->source_name[NAME_MAX_LENGTH - 1] = '\0';
    strncpy(src->source_path, path, SOURCE_MAX_LENGTH);
    src->source_path[SOURCE_MAX_LENGTH - 1] = '\0';
}

//initialise un schema en remplissant son nom et en réinitialisant l'index des colonnes
void schema_init(Schema *schema, const char *name) {
    if (!schema) {
        return;
    }
    schema->last_index = -1;
    strncpy(schema->schema_name, name, NAME_MAX_LENGTH);
    schema->schema_name[NAME_MAX_LENGTH - 1] = '\0';
}

//rajoute une colonne au schema, retourne son index(dernier index) ou -1 en cas d'erreur
int schema_add_column(Schema *schema, const char *name, VariableType type) {
    if (!schema || !name) {
        return -1;
    }
    if (schema->last_index + 1 >= SCHEMA_MAX_VARIABLES) {
        fprintf(stderr, "schema column overflow\n");
        return -1;
    }
    ++schema->last_index;
    strncpy(schema->column_names[schema->last_index], name, SCHEMA_MAX_STRING_LENGTH);
    schema->column_names[schema->last_index][SCHEMA_MAX_STRING_LENGTH - 1] = '\0';
    schema->column_types[schema->last_index] = type;
    return schema->last_index;
}

//copie les colonnes d'un schema source vers un schema destination, retourne 0 en cas de succès, -1 en cas d'erreur
int schema_copy_columns(Schema *dest, const Schema *source) {
    if (!dest || !source) {
        return -1;
    }
    for (int i = 0; i <= source->last_index; ++i) {
        const char *col_name = source->column_names[i];
        if (schema_find_column(dest, col_name) < 0) {
            if (schema_add_column(dest, col_name, source->column_types[i]) < 0) {
                return -1;
            }
        }
    }
    return 0;
}

//initialise une association en remplissant les noms de la source et du schema
void association_init(Association *assoc, const char *src, const char *schema) {
    if (!assoc) {
        return;
    }
    strncpy(assoc->source_name, src, NAME_MAX_LENGTH);
    assoc->source_name[NAME_MAX_LENGTH - 1] = '\0';
    strncpy(assoc->schema_name, schema, NAME_MAX_LENGTH);
    assoc->schema_name[NAME_MAX_LENGTH - 1] = '\0';
}

//init la table des symboles globales
void globals_init(GlobalSymbols *globals) {
    if (!globals) {
        return;
    }
    symtable_init(&globals->identifiers);
    globals->source_count = 0;
    globals->schema_count = 0;
    globals->association_count = 0;
}

//ajouter une source a la table des symboles globales, retourne un pointeur vers la source ou NULL en cas d'erreur
Source *globals_add_source(GlobalSymbols *globals, const char *name, const char *path, int *out_error) {
    if (out_error) {
        *out_error = 0;
    }
    if (!globals || !name || !path) {
        if (out_error) *out_error = -1;
        return NULL;
    }
    if (globals->source_count >= SYMTABLE_MAX_SYMBOLS) {
        if (out_error) *out_error = -2;
        return NULL;
    }
    int existing = symtable_find(&globals->identifiers, name);
    if (existing >= 0) {
        if (out_error) *out_error = -3;
        return NULL;
    }
    symtable_add(&globals->identifiers, name);
    Source *slot = &globals->sources[globals->source_count++];
    source_init(slot, name, path);
    return slot;
}

//ajouter un schema a la table des symboles globales, retourne un pointeur vers le schema ou NULL en cas d'erreur
Schema *globals_add_schema(GlobalSymbols *globals, const char *name, int *out_error) {
    if (out_error) {
        *out_error = 0;
    }
    if (!globals || !name) {
        if (out_error) *out_error = -1;
        return NULL;
    }
    if (globals->schema_count >= SYMTABLE_MAX_SYMBOLS) {
        if (out_error) *out_error = -2;
        return NULL;
    }
    if (symtable_find(&globals->identifiers, name) >= 0) {
        if (out_error) *out_error = -3;
        return NULL;
    }
    symtable_add(&globals->identifiers, name);
    Schema *slot = &globals->schemas[globals->schema_count++];
    schema_init(slot, name);
    return slot;
}

//ajoute une colonne a un schema dans la table des symboles globales, retourne 0 en cas de succès, -1 en cas d'erreur
int globals_add_schema_column(GlobalSymbols *globals, const char *schema_name, const char *col_name, VariableType type, char **out_error_msg) {
    if (out_error_msg) {
        *out_error_msg = NULL;
    }
    if (!globals || !schema_name || !col_name) {
        if (out_error_msg) *out_error_msg = sym_strdup("internal error (null pointer)");
        return -1;
    }
    const Schema *schema = globals_find_schema(globals, schema_name);
    if (!schema) {
        if (out_error_msg) {
            size_t len = strlen(schema_name) + 64;
            char *msg = (char *)malloc(len);
            if (msg) {
                snprintf(msg, len, "schema '%s' is not declared", schema_name);
                *out_error_msg = msg;
            }
        }
        return -1;
    }

    Schema *mutable_schema = globals_find_schema_mutable(globals, schema_name);
    if (!mutable_schema) {
        return -1;
    }

    for (int i = 0; i <= mutable_schema->last_index; ++i) {
        if (strcmp(mutable_schema->column_names[i], col_name) == 0) {
            if (out_error_msg) {
                size_t len = strlen(col_name) + 64;
                char *msg = (char *)malloc(len);
                if (msg) {
                    snprintf(msg, len, "column '%s' already declared in schema '%s'", col_name, schema_name);
                    *out_error_msg = msg;
                }
            }
            return -1;
        }
    }
    return schema_add_column(mutable_schema, col_name, type);
}

//ajoute une association source-schema a la table des symboles globales, retourne un pointeur vers l'association ou NULL en cas d'erreur
Association *globals_add_association(GlobalSymbols *globals, const char *schema_name, const char *source_name, char **out_error_msg) {
    if (out_error_msg) {
        *out_error_msg = NULL;
    }
    if (!globals || !schema_name || !source_name) {
        if (out_error_msg) *out_error_msg = sym_strdup("internal error (null pointer)");
        return NULL;
    }
    if (globals->association_count >= SYMTABLE_MAX_SYMBOLS) {
        if (out_error_msg) *out_error_msg = sym_strdup("too many associations declared");
        return NULL;
    }
    if (!globals_find_schema(globals, schema_name)) {
        if (out_error_msg) {
            size_t len = strlen(schema_name) + 64;
            char *msg = (char *)malloc(len);
            if (msg) {
                snprintf(msg, len, "schema '%s' is not declared", schema_name);
                *out_error_msg = msg;
            }
        }
        return NULL;
    }
    if (!globals_find_source(globals, source_name)) {
        if (out_error_msg) {
            size_t len = strlen(source_name) + 64;
            char *msg = (char *)malloc(len);
            if (msg) {
                snprintf(msg, len, "source '%s' is not declared", source_name);
                *out_error_msg = msg;
            }
        }
        return NULL;
    }
    Association *slot = &globals->associations[globals->association_count++];
    association_init(slot, source_name, schema_name);
    return slot;
}

//cherche une source par son nom dans la table des symboles globales, retourne un pointeur vers la source ou NULL si non trouvée
const Source *globals_find_source(const GlobalSymbols *globals, const char *name) {
    if (!globals || !name) {
        return NULL;
    }
    for (int i = 0; i < globals->source_count; ++i) {
        if (strcmp(globals->sources[i].source_name, name) == 0) {
            return &globals->sources[i];
        }
    }
    return NULL;
}

//cherche un schema par son nom dans la table des symboles globales, retourne un pointeur vers le schema ou NULL si non trouvée
const Schema *globals_find_schema(const GlobalSymbols *globals, const char *name) {
    if (!globals || !name) {
        return NULL;
    }
    for (int i = 0; i < globals->schema_count; ++i) {
        if (strcmp(globals->schemas[i].schema_name, name) == 0) {
            return &globals->schemas[i];
        }
    }
    return NULL;
}

//cherche un schema par son nom dans la table des symboles globales, retourne un pointeur mutable vers le schema ou NULL si non trouvée
//mutable permet de modifier le schema retourné directement dans la table des symboles globales
Schema *globals_find_schema_mutable(GlobalSymbols *globals, const char *name) {
    if (!globals || !name) {
        return NULL;
    }
    for (int i = 0; i < globals->schema_count; ++i) {
        if (strcmp(globals->schemas[i].schema_name, name) == 0) {
            return &globals->schemas[i];
        }
    }
    return NULL;
}

//cherche le schema associé à une source dans la table des symboles globales, retourne un pointeur vers le schema ou NULL si non trouvée
const Schema *globals_schema_for_source(const GlobalSymbols *globals, const char *source_name) {
    if (!globals || !source_name) {
        return NULL;
    }
    for (int i = 0; i < globals->association_count; ++i) {
        if (strcmp(globals->associations[i].source_name, source_name) == 0) {
            return globals_find_schema(globals, globals->associations[i].schema_name);
        }
    }
    return NULL;
}

//cherche une colonne dans un schema, retourne l'index de la colonne ou -1 si non trouvée
int schema_find_column(const Schema *schema, const char *column_name) {
    if (!schema || !column_name) {
        return -1;
    }
    for (int i = 0; i <= schema->last_index; ++i) {
        if (strcmp(schema->column_names[i], column_name) == 0) {
            return i;
        }
    }
    return -1;
}

//cherche le type d'une colonne dans un schema, retourne le type de la colonne ou VAR_INT si non trouvée
VariableType schema_column_type(const Schema *schema, const char *column_name, int *found) {
    if (found) {
        *found = 0;
    }
    int idx = schema_find_column(schema, column_name);
    if (idx < 0) {
        return VAR_INT;
    }
    if (found) {
        *found = 1;
    }
    return schema->column_types[idx];
}

//cherche le type d'une colonne dans la table des symboles globales, retourne le type de la colonne ou VAR_INT si non trouvée
VariableType globals_find_column_type(const GlobalSymbols *globals, const char *column_name, int *found) {
    if (found) {
        *found = 0;
    }
    if (!globals || !column_name) {
        return VAR_INT;
    }
    for (int i = 0; i < globals->schema_count; ++i) {
        int local_found = 0;
        VariableType t = schema_column_type(&globals->schemas[i], column_name, &local_found);
        if (local_found) {
            if (found) {
                *found = 1;
            }
            return t;
        }
    }
    return VAR_INT;
}

// print de la table des symboles globales (sources, schemas, associations) pour debug
void globals_dump(const GlobalSymbols *globals, FILE *out) {
    if (!globals || !out) {
        return;
    }

    fprintf(out, "Sources (%d)\n", globals->source_count);
    for (int i = 0; i < globals->source_count; ++i) {
        fprintf(out, "  [%d] %s -> %s\n", i, globals->sources[i].source_name, globals->sources[i].source_path);
    }

    fprintf(out, "Schemas (%d)\n", globals->schema_count);
    for (int i = 0; i < globals->schema_count; ++i) {
        const Schema *schema = &globals->schemas[i];
        fprintf(out, "  [%d] %s\n", i, schema->schema_name);
        for (int col = 0; col <= schema->last_index; ++col) {
            const char *type_str = NULL;
            switch (schema->column_types[col]) {
            case VAR_INT: type_str = "int"; break;
            case VAR_FLOAT: type_str = "float"; break;
            case VAR_STRING: type_str = "string"; break;
            case VAR_BOOL: type_str = "bool"; break;
            }
            fprintf(out, "      - %s : %s\n", schema->column_names[col], type_str ? type_str : "unknown");
        }
    }

    fprintf(out, "Associations (%d)\n", globals->association_count);
    for (int i = 0; i < globals->association_count; ++i) {
        fprintf(out, "  [%d] %s <-> %s\n", i, globals->associations[i].schema_name, globals->associations[i].source_name);
    }
}
