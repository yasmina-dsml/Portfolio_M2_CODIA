#ifndef SYMTABLE_H
#define SYMTABLE_H

/* Lightweight symbol table helpers; will be extended during semantic pass. */

#include <stddef.h>
#include <stdio.h>

#define SYMTABLE_MAX_SYMBOLS        100
#define SYMTABLE_MAX_LENGTH         50
#define NAME_MAX_LENGTH             50
#define SOURCE_MAX_LENGTH           100
#define SCHEMA_MAX_VARIABLES        64
#define SCHEMA_MAX_STRING_LENGTH    50

//types de variables supportés en afstat
typedef enum VariableType {
    VAR_INT = 0,
    VAR_FLOAT,
    VAR_STRING,
    VAR_BOOL,
} VariableType;

// structure de la table des symboles : un tab 2D pour les entrees et index du dernier element
typedef struct SymbolTable {
    char entries[SYMTABLE_MAX_SYMBOLS][SYMTABLE_MAX_LENGTH];
    int last_index;
} SymbolTable;

// structure pour les sources de donnees pour stocker dans la table des symboles 
typedef struct Source {
    char source_name[NAME_MAX_LENGTH];
    char source_path[SOURCE_MAX_LENGTH];
} Source;

// structure pour les schemas de donnees pour stocker dans la table des symboles
typedef struct Schema {
    char schema_name[NAME_MAX_LENGTH];
    char column_names[SCHEMA_MAX_VARIABLES][SCHEMA_MAX_STRING_LENGTH];
    VariableType column_types[SCHEMA_MAX_VARIABLES];
    int last_index;
} Schema;

// structure pour les associations source-schema
typedef struct Association {
    char source_name[NAME_MAX_LENGTH];
    char schema_name[NAME_MAX_LENGTH];
} Association;

// structure pour les symboles globaux : table des symboles + tableaux de sources, schemas, associations + compteurs
typedef struct GlobalSymbols {
    SymbolTable identifiers;
    Source sources[SYMTABLE_MAX_SYMBOLS];
    Schema schemas[SYMTABLE_MAX_SYMBOLS];
    Association associations[SYMTABLE_MAX_SYMBOLS];
    int source_count;
    int schema_count;
    int association_count;
} GlobalSymbols;

void symtable_init(SymbolTable *table);
int symtable_find(const SymbolTable *table, const char *symbol);
int symtable_add(SymbolTable *table, const char *symbol);
void symtable_dump(const SymbolTable *table);
void symtable_destroy(SymbolTable *table);

void source_init(Source *src, const char *name, const char *path);
void schema_init(Schema *schema, const char *name);
int schema_add_column(Schema *schema, const char *name, VariableType type);
int schema_copy_columns(Schema *dest, const Schema *source);
void association_init(Association *assoc, const char *src, const char *schema);

void globals_init(GlobalSymbols *globals);
Source *globals_add_source(GlobalSymbols *globals, const char *name, const char *path, int *out_error);
Schema *globals_add_schema(GlobalSymbols *globals, const char *name, int *out_error);
int globals_add_schema_column(GlobalSymbols *globals, const char *schema_name, const char *col_name, VariableType type, char **out_error_msg);
Association *globals_add_association(GlobalSymbols *globals, const char *schema_name, const char *source_name, char **out_error_msg);
const Source *globals_find_source(const GlobalSymbols *globals, const char *name);
const Schema *globals_find_schema(const GlobalSymbols *globals, const char *name);
Schema *globals_find_schema_mutable(GlobalSymbols *globals, const char *name);
const Schema *globals_schema_for_source(const GlobalSymbols *globals, const char *source_name);
int schema_find_column(const Schema *schema, const char *column_name);
VariableType schema_column_type(const Schema *schema, const char *column_name, int *found);
VariableType globals_find_column_type(const GlobalSymbols *globals, const char *column_name, int *found);
void globals_dump(const GlobalSymbols *globals, FILE *out);

#endif
