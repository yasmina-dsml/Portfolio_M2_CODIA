%{
/* Bison grammar for the AFSTAT DSL. Strictly syntax-oriented for now. */
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <errno.h>
#include <stdbool.h>
#include <ctype.h>

#include "ast.h"
#include "symtable.h"

extern int yylex(void); //appelle le lexeur pour obtenir next token dans l'analyse syntaxique
extern FILE *yyin; //fichier d'entrée pour le lexeur

#ifndef AFSTAT_CODEGEN_TYPES
#define AFSTAT_CODEGEN_TYPES

//operations statistiques pris en charge dans analyze
typedef enum AnalyzeOp {
    ANALYZE_OP_MEAN = 0,
    ANALYZE_OP_MEDIAN,
    ANALYZE_OP_STDDEV,
    ANALYZE_OP_MIN,
    ANALYZE_OP_MAX,
    ANALYZE_OP_HISTOGRAM,
    ANALYZE_OP_SUMMARY
} AnalyzeOp;

// Types d'expressions pour generer le code 
typedef enum CodeExprKind {
    CODE_EXPR_LITERAL_INT = 0, //exp : 42
    CODE_EXPR_LITERAL_FLOAT, //exp : 3.14
    CODE_EXPR_LITERAL_STRING, //exp : "hello"
    CODE_EXPR_COLUMN, //exp : column reference
    CODE_EXPR_BINARY, //exp : binary operation (e.g., a + b)
    CODE_EXPR_UNARY_NEG //exp : unary negation (-a)
} CodeExprKind;

// structure pour representer une expression dans le code
typedef struct CodeExpr {
    CodeExprKind kind;
    VariableType type;
    union {
        long long int_val;
        double float_val;
        char *str_val;
        struct {
            char *schema_name;
            char *column_name;
            int column_index;
        } column;
        struct {
            char op[4];
            struct CodeExpr *lhs;
            struct CodeExpr *rhs;
        } binary;
        struct {
            struct CodeExpr *child;
        } unary;
    } data;
} CodeExpr; //exp: 42, exp: a + b, exp: -c etc.


// infos d'une expression : le noeud AST, type de variable, validite, expression de code associee
typedef struct ExprInfo {
    ASTNode *node;
    VariableType type;
    int valid;
    CodeExpr *code;
} ExprInfo;

// reference pour une jointure pour connaitre le schema, colonne, type etc , ca sert a valider les jointures
typedef struct JoinRef {
    char *dataset;
    char *column;
    const Schema *schema;
    VariableType type;
    int column_index;
    int valid;
} JoinRef;

// request de calcul pour stocker les infos d'une operation compute afin de generer le code plus tard
typedef struct ComputeRequest {
    char schema_name[NAME_MAX_LENGTH];
    char column_name[SCHEMA_MAX_STRING_LENGTH];
    VariableType result_type;
    CodeExpr *expr;
    int column_index;
} ComputeRequest;

// request d'analyse pour stocker les infos d'une operation analyze afin de generer le code plus tard
typedef struct AnalyzeRequest {
    char schema_name[NAME_MAX_LENGTH];
    char column_name[SCHEMA_MAX_STRING_LENGTH];
    AnalyzeOp op;
    VariableType column_type;
    int column_index;
} AnalyzeRequest;

// request de filtrage pour stocker les infos d'une operation filter afin de generer le code plus tard
typedef struct FilterRequest {
    char target_schema[NAME_MAX_LENGTH];
    char source_schema[NAME_MAX_LENGTH];
    CodeExpr *condition;
} FilterRequest;

// request de jointure pour stocker les infos d'une operation join afin de generer le code plus tard
typedef struct JoinRequest {
    char target_schema[NAME_MAX_LENGTH];
    char left_schema[NAME_MAX_LENGTH];
    char right_schema[NAME_MAX_LENGTH];
    int left_column_index;
    int right_column_index;
    VariableType join_type;
    int left_column_count;
    int right_column_count;
} JoinRequest;

// relier un schema a un id de table pour la generation de code
typedef struct SchemaTableBinding {
    const Schema *schema;
    char table_ident[NAME_MAX_LENGTH * 2 + 16];
} SchemaTableBinding;

// relier plusieurs schemas a leurs tables respectives pour la generation de code
typedef struct SchemaTableMap {
    SchemaTableBinding entries[SYMTABLE_MAX_SYMBOLS];
    int count;
} SchemaTableMap;
#endif

/* lignes commentés sinon redondance mais pas osé les supprimer jusqu'a etre sur
typedef struct ExprInfo ExprInfo;
typedef struct JoinRef JoinRef;
typedef struct CodeExpr CodeExpr;
typedef enum AnalyzeOp AnalyzeOp;
typedef struct SchemaTableMap SchemaTableMap;
typedef struct SchemaTableBinding SchemaTableBinding;
typedef struct ComputeRequest ComputeRequest;
typedef struct AnalyzeRequest AnalyzeRequest;
typedef struct FilterRequest FilterRequest;
*/

// fonctions utilitaires pour le parsing, la gestion des erreurs, la creation d'expressions etc.
static void yyerror(const char *msg);
static void parser_log(const char *stage);
static void semantic_error(const char *msg);
static char *unquote_string(const char *text);
static char *dup_text(const char *text);
static int expr_is_numeric_type(VariableType type);
static VariableType expr_promote_numeric(VariableType a, VariableType b);
static struct CodeExpr *code_expr_new_literal_int(long long value);
static struct CodeExpr *code_expr_new_literal_float(double value);
static struct CodeExpr *code_expr_new_literal_string(const char *value);
static struct CodeExpr *code_expr_new_column(const char *schema_name, const char *column_name, int column_index, VariableType type);
static struct CodeExpr *code_expr_new_binary(const char *op, struct CodeExpr *lhs, struct CodeExpr *rhs, VariableType type);
static struct CodeExpr *code_expr_new_unary_neg(struct CodeExpr *child, VariableType type);
static ExprInfo expr_make(ASTNode *node, VariableType type, struct CodeExpr *code);
static ExprInfo expr_error(void);
static ExprInfo expr_handle_arithmetic(ExprInfo left, ExprInfo right, const char *op);
static ExprInfo expr_handle_comparison(ExprInfo left, ExprInfo right, const char *op, int ordered);
static int join_types_compatible(VariableType a, VariableType b);
static ExprInfo make_identifier_expr(const char *ident);
static ExprInfo make_qualified_expr(const char *dataset, const char *column);
static int record_compute_request(const Schema *schema, const char *column_name, VariableType result_type, struct CodeExpr *expr);
static void make_c_identifier(const char *src, char *dst, size_t dst_size);
static const char *runtime_literal_for_type(VariableType type);
static int path_is_absolute(const char *path);
static const char *codegen_resolve_csv_path(const char *raw, char *buffer, size_t buffer_size);
static void free_code_expr(struct CodeExpr *expr);
static int resolve_column_reference(const char *dataset, const char *column, const Schema **out_schema, int *out_index, VariableType *out_type);
static int schema_num_computed_columns(const Schema *schema);
static int schema_base_column_count(const Schema *schema);
static void build_schema_table_map(SchemaTableMap *map);
static const char *codegen_lookup_table_base(const char *schema_name, void *userdata);
static const char *c_type_for_var(VariableType type);
static void release_compute_requests(void);
static void release_analyze_requests(void);
static void release_filter_requests(void);
static void release_join_requests(void);
static int record_analyze_request(const Schema *schema, const char *column_name, AnalyzeOp op, VariableType column_type, int column_index);
static int record_filter_request(const char *target_schema, const char *source_schema, CodeExpr *condition);
static int record_join_request(const Schema *target, const Schema *left, int left_col_index, const Schema *right, int right_col_index, VariableType join_type);
static const char *analyze_label(AnalyzeOp op);
static void reset_codegen_state(void);
static int is_equality_operator(const char *op);
static void emit_c_string_literal(FILE *out, const char *value);

int g_emit_tokens = 1;
static int g_emit_parser_logs = 1;
static int g_dump_symbol_table = 0;
static const char *g_output_path = NULL;
static int g_syntax_only = 0;
static const char *g_version_info = "AFSTAT compiler (2025-26) - Yasmina Farid, Yassine Fadl, Lenny Borry, Adrien Criseo";

static GlobalSymbols globals;
static Schema *current_schema = NULL;
static Schema *current_dataset_schema = NULL;
static const Schema *current_analyze_schema = NULL;

#define MAX_COMPUTE_REQUESTS 128
static ComputeRequest compute_requests[MAX_COMPUTE_REQUESTS];
static int compute_request_count = 0;
#define MAX_ANALYZE_REQUESTS 256
static AnalyzeRequest analyze_requests[MAX_ANALYZE_REQUESTS];
static int analyze_request_count = 0;
#define MAX_FILTER_REQUESTS 128
static FilterRequest filter_requests[MAX_FILTER_REQUESTS];
static int filter_request_count = 0;
#define MAX_JOIN_REQUESTS 128
static JoinRequest join_requests[MAX_JOIN_REQUESTS];
static int join_request_count = 0;

%}

%define parse.error verbose

// code requires pour recopier les typedef et union dans le parser.tab.h sinon il compile pas
%code requires {
#ifndef AFSTAT_CODEGEN_TYPES
#define AFSTAT_CODEGEN_TYPES
typedef enum AnalyzeOp {
    ANALYZE_OP_MEAN = 0,
    ANALYZE_OP_MEDIAN,
    ANALYZE_OP_STDDEV,
    ANALYZE_OP_MIN,
    ANALYZE_OP_MAX,
    ANALYZE_OP_HISTOGRAM,
    ANALYZE_OP_SUMMARY
} AnalyzeOp;

typedef enum CodeExprKind {
    CODE_EXPR_LITERAL_INT = 0,
    CODE_EXPR_LITERAL_FLOAT,
    CODE_EXPR_LITERAL_STRING,
    CODE_EXPR_COLUMN,
    CODE_EXPR_BINARY,
    CODE_EXPR_UNARY_NEG
} CodeExprKind;

typedef struct CodeExpr {
    CodeExprKind kind;
    VariableType type;
    union {
        long long int_val;
        double float_val;
        char *str_val;
        struct {
            char *schema_name;
            char *column_name;
            int column_index;
        } column;
        struct {
            char op[4];
            struct CodeExpr *lhs;
            struct CodeExpr *rhs;
        } binary;
        struct {
            struct CodeExpr *child;
        } unary;
    } data;
} CodeExpr;

typedef struct ExprInfo {
    ASTNode *node;
    VariableType type;
    int valid;
    CodeExpr *code;
} ExprInfo;

typedef struct JoinRef {
    char *dataset;
    char *column;
    const Schema *schema;
    VariableType type;
    int valid;
} JoinRef;

typedef struct ComputeRequest {
    char schema_name[NAME_MAX_LENGTH];
    char column_name[SCHEMA_MAX_STRING_LENGTH];
    VariableType result_type;
    CodeExpr *expr;
    int column_index;
} ComputeRequest;

typedef struct SchemaTableBinding {
    const Schema *schema;
    char table_ident[NAME_MAX_LENGTH * 2 + 16];
} SchemaTableBinding;

typedef struct SchemaTableMap {
    SchemaTableBinding entries[SYMTABLE_MAX_SYMBOLS];
    int count;
} SchemaTableMap;
#endif
}

%union {
    /* struct pointer placeholders until real AST nodes are wired in. */
    ASTNode *node;
    char *str;
    long long int_val;
    double float_val;
    VariableType var_type;
    AnalyzeOp analyze_op;
    ExprInfo expr;
    JoinRef join_ref;
}

%destructor { ast_free($$); } <node>
%destructor { free($$); } <str>
%destructor { if ($$.node) ast_free($$.node); free_code_expr($$.code); } <expr>
%destructor { free($$.dataset); free($$.column); } <join_ref>

/* Terminal catalog – numeric values align with tokens.h for consistency. */
%token TOKEN_SOURCE       256
%token TOKEN_SCHEMA       257
%token TOKEN_ASSOCIATE    258
%token TOKEN_WITH         259
%token TOKEN_COMPUTE      260
%token TOKEN_ANALYZE      261
%token TOKEN_JOIN         262
%token TOKEN_INNER        263
%token TOKEN_FILTER       264
%token TOKEN_WHERE        265
%token TOKEN_MEAN         266
%token TOKEN_MEDIAN       267
%token TOKEN_STDDEV       268
%token TOKEN_MIN          269
%token TOKEN_MAX          270
%token TOKEN_HISTOGRAM    271
%token TOKEN_SUMMARY      272
%token TOKEN_INTEGER      273
%token TOKEN_FLOAT        274
%token TOKEN_STRING       275
%token TOKEN_LBRACE       276
%token TOKEN_RBRACE       277
%token TOKEN_SEMI         278
%token TOKEN_DOT          279
%token TOKEN_ASSIGN       280
%token TOKEN_EQ           281
%token TOKEN_NEQ          282
%token TOKEN_LT           283
%token TOKEN_GT           284
%token TOKEN_LE           285
%token TOKEN_GE           286
%token TOKEN_PLUS         287
%token TOKEN_MINUS        288
%token TOKEN_MUL          289
%token TOKEN_DIV          290
%token TOKEN_LPAREN       291
%token TOKEN_RPAREN       292
%token <int_val>   TOKEN_INT_LIT    293
%token <float_val> TOKEN_FLOAT_LIT  294
%token <str>       TOKEN_STRING_LIT 295
%token <str>       TOKEN_IDENT      296

/* NonTerminal categories that currently return AST placeholders */
%type <node> program
%type <node> source_section source_decl
%type <node> declaration_section declaration schema_decl schema_field_list schema_field association_decl
%type <node> operation_section operation filter_stmt compute_stmt join_stmt
%type <join_ref> join_source
%type <expr> condition
%type <expr> expression term factor literal qualified_ident
%type <node> analyze_section analyze_block analyze_item_list analyze_item
%type <analyze_op> analyze_op
%type <var_type> type_token

%left TOKEN_PLUS TOKEN_MINUS
%left TOKEN_MUL TOKEN_DIV
%precedence UMINUS

%start program

%%

/* Global layout: sources -> declarations -> optional ops -> analyses */
program
    : source_section declaration_section operation_section analyze_section
      {
          parser_log("program structure recognized");
          ast_free($1);
          ast_free($2);
          ast_free($3);
          ast_free($4);
          $$ = ast_make_node(AST_NODE_PROGRAM);
      }
    ;

/* A program must declare at least 1 source, allow repetitions */
source_section
    : source_section source_decl
      {
          ast_free($2);
          $$ = $1;
      }
    | source_decl
      {
          $$ = $1;
      }
    ;

/* source name "path"; */
source_decl
    : TOKEN_SOURCE TOKEN_IDENT TOKEN_STRING_LIT TOKEN_SEMI // exp : source patients "data/patients.csv";
      {
          parser_log("source declaration");
          int err = 0;
          char *unquoted = unquote_string($3);
          const char *path = unquoted ? unquoted : $3;
          if (!globals_add_source(&globals, $2, path, &err)) { // ajouter source dans table des symboles globale
              switch (err) {
              case -2:
                  semantic_error("too many sources declared");
                  break;
              case -3:
                  {
                      size_t len = strlen($2) + 32;
                      char *msg = (char *)malloc(len);
                      if (msg) {
                          snprintf(msg, len, "identifier '%s' already declared", $2);
                          semantic_error(msg);
                          free(msg);
                      } else {
                          semantic_error("identifier already declared");
                      }
                  }
                  break;
              default:
                  semantic_error("invalid source declaration");
                  break;
              }
              free(unquoted);
              YYERROR;
          }
          free(unquoted);
          $$ = ast_make_node(AST_NODE_SOURCE_DECL);
      }
    ;

/* Schemas and associations may interleave, so they share this block */
declaration_section
    : declaration_section declaration
      {
          ast_free($2);
          $$ = $1;
      }
    | declaration
      {
          $$ = $1;
      }
    ;

declaration
    : schema_decl
      {
          $$ = $1;
      }
    | association_decl
      {
          $$ = $1;
      }
    ;

/* schema <id> { column type; ... } */
schema_decl
    : TOKEN_SCHEMA TOKEN_IDENT
      {
          parser_log("schema declaration");
          int err = 0;
          current_schema = globals_add_schema(&globals, $2, &err);
          if (!current_schema) {
              switch (err) {
              case -2:
                  semantic_error("too many schemas declared");
                  break;
              case -3:
                  {
                      size_t len = strlen($2) + 32;
                      char *msg = (char *)malloc(len);
                      if (msg) {
                          snprintf(msg, len, "identifier '%s' already declared", $2);
                          semantic_error(msg);
                          free(msg);
                      } else {
                          semantic_error("identifier already declared");
                      }
                  }
                  break;
              default:
                  semantic_error("invalid schema declaration");
                  break;
              }
              YYERROR;
          }
      }
      TOKEN_LBRACE schema_field_list TOKEN_RBRACE
      {
          current_schema = NULL;
          ast_free($5);
          $$ = ast_make_node(AST_NODE_SCHEMA_DECL);
      }
    ;

schema_field_list
    : schema_field_list schema_field
      {
          ast_free($2);
          $$ = $1;
      }
    | schema_field
      {
          $$ = $1;
      }
    ;

/* column_name <type>; */
schema_field
    : TOKEN_IDENT type_token TOKEN_SEMI
      {
          if (!current_schema) {
              semantic_error("internal error: no active schema");
              YYERROR;
          }
          char *err = NULL;
          if (globals_add_schema_column(&globals, current_schema->schema_name, $1, $2, &err) < 0) {
              if (err) {
                  semantic_error(err);
                  free(err);
              } else {
                  semantic_error("invalid schema column");
              }
              YYERROR;
          }
          $$ = ast_make_node(AST_NODE_SCHEMA_DECL);
      }
    ;

type_token
    : TOKEN_INTEGER
      {
          $$ = VAR_INT;
      }
    | TOKEN_FLOAT
      {
          $$ = VAR_FLOAT;
      }
    | TOKEN_STRING
      {
          $$ = VAR_STRING;
      }
    ;

/* associate schema with source */
association_decl
    : TOKEN_ASSOCIATE TOKEN_IDENT TOKEN_WITH TOKEN_IDENT TOKEN_SEMI
      {
          parser_log("association declaration");
          char *err = NULL;
          if (!globals_add_association(&globals, $2, $4, &err)) {
              if (err) {
                  semantic_error(err);
                  free(err);
              } else {
                  semantic_error("invalid association");
              }
              YYERROR;
          }
          Schema *assoc_schema = globals_find_schema_mutable(&globals, $2);
          if (assoc_schema) {
              current_dataset_schema = assoc_schema;
          }
          $$ = ast_make_node(AST_NODE_ASSOCIATE_DECL);
      }
    ;

/* Operations are optional and may repeat (join/filter/compute) */
operation_section
    : operation_section operation
      {
          ast_free($2);
          $$ = $1;
      }
    | /* empty */
      {
          $$ = NULL;
      }
    ;

/* Wrapper union so callers see each operation as a standalone node */
operation
    : join_stmt
      {
          $$ = $1;
      }
    | filter_stmt
      {
          $$ = $1;
      }
    | compute_stmt
      {
          $$ = $1;
      }
    ;

/* join name = left.inner join right */
join_stmt
    : TOKEN_JOIN TOKEN_IDENT TOKEN_ASSIGN join_source TOKEN_INNER TOKEN_JOIN join_source TOKEN_SEMI //exp : join joined_data = patients.id inner join visits_info.patient_id;
      {
          parser_log("join statement");
          if (!$4.valid || !$7.valid) {
              semantic_error("invalid join operands");
              YYERROR;
          }
          if (!join_types_compatible($4.type, $7.type)) {
              semantic_error("join columns have incompatible types");
              YYERROR;
          }
          int add_err = 0;
          Schema *join_schema = globals_add_schema(&globals, $2, &add_err);
          if (!join_schema) {
              switch (add_err) {
              case -2:
                  semantic_error("too many schemas declared");
                  break;
              case -3:
                  {
                      size_t len = strlen($2) + 32;
                      char *msg = (char *)malloc(len);
                      if (msg) {
                          snprintf(msg, len, "identifier '%s' already declared", $2);
                          semantic_error(msg);
                          free(msg);
                      } else {
                          semantic_error("identifier already declared");
                      }
                  }
                  break;
              default:
                  semantic_error("invalid join declaration");
                  break;
              }
              YYERROR;
          }
          int left_index = -1;
          if ($4.schema) {
              left_index = schema_find_column($4.schema, $4.column);
              if (left_index < 0) {
                  semantic_error("internal error: join column not found in left schema");
                  YYERROR;
              }
              if (schema_copy_columns(join_schema, $4.schema) < 0) {
                  semantic_error("unable to copy columns for join");
                  YYERROR;
              }
          }
          int right_index = -1;
          if ($7.schema) {
              right_index = schema_find_column($7.schema, $7.column);
              if (right_index < 0) {
                  semantic_error("internal error: join column not found in right schema");
                  YYERROR;
              }
              if (schema_copy_columns(join_schema, $7.schema) < 0) {
                  semantic_error("unable to copy columns for join");
                  YYERROR;
              }
          }
          if (record_join_request(join_schema, $4.schema, left_index, $7.schema, right_index, $4.type) != 0) {
              YYERROR;
          }
          current_dataset_schema = join_schema;
          $$ = ast_make_node(AST_NODE_JOIN_STMT);
      }
    ;

join_source
    : TOKEN_IDENT TOKEN_DOT TOKEN_IDENT
      {
          JoinRef ref;
          ref.dataset = dup_text($1);
          ref.column = dup_text($3);
          ref.schema = NULL;
          ref.type = VAR_INT;
          ref.valid = 0;
          const Schema *schema = globals_find_schema(&globals, $1);
          if (!schema) {
              schema = globals_schema_for_source(&globals, $1);
          }
          if (!schema) {
              size_t len = strlen($1) + 64;
              char *msg = (char *)malloc(len);
              if (msg) {
                  snprintf(msg, len, "dataset '%s' is not declared", $1);
                  semantic_error(msg);
                  free(msg);
              } else {
                  semantic_error("dataset is not declared");
              }
              ref.valid = 0;
          } else {
              int found = 0;
              VariableType t = schema_column_type(schema, $3, &found);
              if (!found) {
                  size_t len = strlen($3) + strlen($1) + 64;
                  char *msg = (char *)malloc(len);
                  if (msg) {
                      snprintf(msg, len, "column '%s' not found in schema '%s'", $3, schema->schema_name);
                      semantic_error(msg);
                      free(msg);
                  } else {
                      semantic_error("column not found in join source");
                  }
                  ref.valid = 0;
              } else {
                  ref.schema = schema;
                  ref.type = t;
                  ref.column_index = schema_find_column(schema, $3);
                  ref.valid = 1;
              }
          }
          $$ = ref;
      }
    ;

/* filter name = source where expr */
filter_stmt
    : TOKEN_FILTER TOKEN_IDENT TOKEN_ASSIGN TOKEN_IDENT TOKEN_WHERE condition TOKEN_SEMI //exp : filter adult_patients = patients where patients.age >= 18;
      {
          parser_log("filter statement");
          const Schema *input_schema = globals_find_schema(&globals, $4);
          if (!input_schema) {
              input_schema = globals_schema_for_source(&globals, $4);
          }
          if (!input_schema) {
              size_t len = strlen($4) + 64;
              char *msg = (char *)malloc(len);
              if (msg) {
                  snprintf(msg, len, "dataset '%s' is not declared", $4);
                  semantic_error(msg);
                  free(msg);
              } else {
                  semantic_error("dataset is not declared");
              }
              YYERROR;
          }
          if (!$6.valid || $6.type != VAR_BOOL) {
              semantic_error("filter condition must be boolean");
              YYERROR;
          }
          int add_err = 0;
          Schema *filter_schema = globals_add_schema(&globals, $2, &add_err);
          if (!filter_schema) {
              switch (add_err) {
              case -2:
                  semantic_error("too many schemas declared");
                  break;
              case -3:
                  {
                      size_t len = strlen($2) + 32;
                      char *msg = (char *)malloc(len);
                      if (msg) {
                          snprintf(msg, len, "identifier '%s' already declared", $2);
                          semantic_error(msg);
                          free(msg);
                      } else {
                          semantic_error("identifier already declared");
                      }
                  }
                  break;
              default:
                  semantic_error("invalid filter declaration");
                  break;
              }
              YYERROR;
          }
          if (schema_copy_columns(filter_schema, input_schema) < 0) {
              semantic_error("unable to copy columns for filter");
              YYERROR;
          }
          CodeExpr *cond_code = $6.code;
          $6.code = NULL;
          if (!cond_code) {
              semantic_error("internal error: missing filter condition");
              YYERROR;
          }
          if (record_filter_request(filter_schema->schema_name, input_schema->schema_name, cond_code) != 0) {
              free_code_expr(cond_code);
              YYERROR;
          }
          current_dataset_schema = filter_schema;
          ast_free($6.node);
          $$ = ast_make_node(AST_NODE_FILTER_STMT);
      }
    ;

/* bool expression based on arithmetic comparisons */
condition
    : expression TOKEN_EQ expression
      {
          $$ = expr_handle_comparison($1, $3, "==", 0);
          if (!$$.valid) {
              YYERROR;
          }
      }
    | expression TOKEN_NEQ expression
      {
          $$ = expr_handle_comparison($1, $3, "!=", 0);
          if (!$$.valid) {
              YYERROR;
          }
      }
    | expression TOKEN_LT expression
      {
          $$ = expr_handle_comparison($1, $3, "<", 1);
          if (!$$.valid) {
              YYERROR;
          }
      }
    | expression TOKEN_GT expression
      {
          $$ = expr_handle_comparison($1, $3, ">", 1);
          if (!$$.valid) {
              YYERROR;
          }
      }
    | expression TOKEN_LE expression
      {
          $$ = expr_handle_comparison($1, $3, "<=", 1);
          if (!$$.valid) {
              YYERROR;
          }
      }
    | expression TOKEN_GE expression
      {
          $$ = expr_handle_comparison($1, $3, ">=", 1);
          if (!$$.valid) {
              YYERROR;
          }
      }
    ;

/* compute name = expression */
compute_stmt
    : TOKEN_COMPUTE TOKEN_IDENT TOKEN_ASSIGN expression TOKEN_SEMI // exp : compute bmi = weight / (height * height);
      {
          parser_log("compute statement");
          if (!current_dataset_schema) {
              semantic_error("compute statement requires a preceding association");
              YYERROR;
          }
          if (!$4.valid || !expr_is_numeric_type($4.type)) {
              semantic_error("compute expression must be numeric");
              YYERROR;
          }
          VariableType result_type = $4.type == VAR_FLOAT ? VAR_FLOAT : VAR_INT;
          char *err = NULL;
          if (globals_add_schema_column(&globals, current_dataset_schema->schema_name, $2, result_type, &err) < 0) {
              if (err) {
                  semantic_error(err);
                  free(err);
              } else {
                  semantic_error("unable to register computed column");
              }
              YYERROR;
          }
          CodeExpr *expr_code = $4.code;
          $4.code = NULL;
          if (!expr_code) {
              semantic_error("internal error: missing compute expression");
              YYERROR;
          }
          if (record_compute_request(current_dataset_schema, $2, result_type, expr_code) != 0) {
              free_code_expr(expr_code);
              YYERROR;
          }
          ast_free($4.node);
          $$ = ast_make_node(AST_NODE_COMPUTE_STMT);
      }
    ;

/* Standard precedence: +,- over terms; terms over factors */
expression
    : expression TOKEN_PLUS term
      {
          ExprInfo result = expr_handle_arithmetic($1, $3, "+");
          if (!result.valid) {
              $$ = result;
              YYERROR;
          }
          CodeExpr *lhs = $1.code;
          CodeExpr *rhs = $3.code;
          $1.code = NULL;
          $3.code = NULL;
          if (!lhs || !rhs) {
              free_code_expr(lhs);
              free_code_expr(rhs);
              semantic_error("internal error: missing operand code for arithmetic expression");
              $$ = expr_error();
              YYERROR;
          }
          CodeExpr *code = code_expr_new_binary("+", lhs, rhs, result.type);
          if (!code) {
              free_code_expr(lhs);
              free_code_expr(rhs);
              semantic_error("internal error: cannot allocate arithmetic expression");
              $$ = expr_error();
              YYERROR;
          }
          result.code = code;
          $$ = result;
      }
    | expression TOKEN_MINUS term
      {
          ExprInfo result = expr_handle_arithmetic($1, $3, "-");
          if (!result.valid) {
              $$ = result;
              YYERROR;
          }
          CodeExpr *lhs = $1.code;
          CodeExpr *rhs = $3.code;
          $1.code = NULL;
          $3.code = NULL;
          if (!lhs || !rhs) {
              free_code_expr(lhs);
              free_code_expr(rhs);
              semantic_error("internal error: missing operand code for arithmetic expression");
              $$ = expr_error();
              YYERROR;
          }
          CodeExpr *code = code_expr_new_binary("-", lhs, rhs, result.type);
          if (!code) {
              free_code_expr(lhs);
              free_code_expr(rhs);
              semantic_error("internal error: cannot allocate arithmetic expression");
              $$ = expr_error();
              YYERROR;
          }
          result.code = code;
          $$ = result;
      }
    | term
      {
          $$ = $1;
      }
    ;

term
    : term TOKEN_MUL factor
      {
          ExprInfo result = expr_handle_arithmetic($1, $3, "*");
          if (!result.valid) {
              $$ = result;
              YYERROR;
          }
          CodeExpr *lhs = $1.code;
          CodeExpr *rhs = $3.code;
          $1.code = NULL;
          $3.code = NULL;
          if (!lhs || !rhs) {
              free_code_expr(lhs);
              free_code_expr(rhs);
              semantic_error("internal error: missing operand code for arithmetic expression");
              $$ = expr_error();
              YYERROR;
          }
          CodeExpr *code = code_expr_new_binary("*", lhs, rhs, result.type);
          if (!code) {
              free_code_expr(lhs);
              free_code_expr(rhs);
              semantic_error("internal error: cannot allocate arithmetic expression");
              $$ = expr_error();
              YYERROR;
          }
          result.code = code;
          $$ = result;
      }
    | term TOKEN_DIV factor
      {
          ExprInfo result = expr_handle_arithmetic($1, $3, "/");
          if (!result.valid) {
              $$ = result;
              YYERROR;
          }
          CodeExpr *lhs = $1.code;
          CodeExpr *rhs = $3.code;
          $1.code = NULL;
          $3.code = NULL;
          if (!lhs || !rhs) {
              free_code_expr(lhs);
              free_code_expr(rhs);
              semantic_error("internal error: missing operand code for arithmetic expression");
              $$ = expr_error();
              YYERROR;
          }
          CodeExpr *code = code_expr_new_binary("/", lhs, rhs, result.type);
          if (!code) {
              free_code_expr(lhs);
              free_code_expr(rhs);
              semantic_error("internal error: cannot allocate arithmetic expression");
              $$ = expr_error();
              YYERROR;
          }
          result.code = code;
          $$ = result;
      }
    | factor
      {
          $$ = $1;
      }
    ;

factor
    : TOKEN_LPAREN expression TOKEN_RPAREN
      {
          $$ = $2;
      }
    | literal
      {
          $$ = $1;
      }
    | qualified_ident
      {
          $$ = $1;
      }
    | TOKEN_MINUS factor %prec UMINUS
      {
          if (!$2.valid || !expr_is_numeric_type($2.type)) {
              semantic_error("unary minus requires a numeric operand");
              $$ = expr_error();
              YYERROR;
          }
          CodeExpr *child = $2.code;
          $2.code = NULL;
          if (!child) {
              semantic_error("internal error: missing operand for unary expression");
              $$ = expr_error();
              YYERROR;
          }
          CodeExpr *code = code_expr_new_unary_neg(child, $2.type);
          if (!code) {
              free_code_expr(child);
              semantic_error("internal error: cannot allocate unary expression");
              $$ = expr_error();
              YYERROR;
          }
          $$ = expr_make(ast_make_node(AST_NODE_EXPRESSION), $2.type, code);
      }
    ;

literal
    : TOKEN_INT_LIT
      {
          CodeExpr *code = code_expr_new_literal_int($1);
          if (!code) {
              semantic_error("internal error: cannot allocate literal expression");
              $$ = expr_error();
              YYERROR;
          }
          $$ = expr_make(ast_make_node(AST_NODE_EXPRESSION), VAR_INT, code);
      }
    | TOKEN_FLOAT_LIT
      {
          CodeExpr *code = code_expr_new_literal_float($1);
          if (!code) {
              semantic_error("internal error: cannot allocate literal expression");
              $$ = expr_error();
              YYERROR;
          }
          $$ = expr_make(ast_make_node(AST_NODE_EXPRESSION), VAR_FLOAT, code);
      }
    | TOKEN_STRING_LIT
      {
          char *unquoted = unquote_string($1);
          const char *text = unquoted ? unquoted : $1;
          CodeExpr *code = code_expr_new_literal_string(text);
          if (unquoted) {
              free(unquoted);
          }
          if (!code) {
              semantic_error("internal error: cannot allocate literal expression");
              $$ = expr_error();
              YYERROR;
          }
          $$ = expr_make(ast_make_node(AST_NODE_EXPRESSION), VAR_STRING, code);
      }
    ;

/* Support dotted identifiers such as source.column */
qualified_ident
    : TOKEN_IDENT TOKEN_DOT TOKEN_IDENT
      {
          $$ = make_qualified_expr($1, $3);
          if (!$$.valid) {
              YYERROR;
          }
      }
    | TOKEN_IDENT
      {
          $$ = make_identifier_expr($1);
          if (!$$.valid) {
              YYERROR;
          }
      }
    ;

/* At least one analyse block is required by the spec */
analyze_section
    : analyze_section analyze_block
      {
          ast_free($2);
          $$ = $1;
      }
    | analyze_block
      {
          $$ = $1;
      }
    ;

analyze_block
    : TOKEN_ANALYZE TOKEN_IDENT
      {
          parser_log("analyze block");
          current_analyze_schema = globals_find_schema(&globals, $2);
          if (!current_analyze_schema) {
              const Schema *schema_from_source = globals_schema_for_source(&globals, $2);
              if (schema_from_source) {
                  current_analyze_schema = schema_from_source;
              } else if (current_dataset_schema) {
                  current_analyze_schema = current_dataset_schema;
              } else {
                  size_t len = strlen($2) + 64;
                  char *msg = (char *)malloc(len);
                  if (msg) {
                      snprintf(msg, len, "analyze target '%s' is not declared", $2);
                      semantic_error(msg);
                      free(msg);
                  } else {
                      semantic_error("analyze target is not declared");
                  }
                  YYERROR;
              }
          }
      }
      TOKEN_LBRACE analyze_item_list TOKEN_RBRACE
      {
          ast_free($5);
          current_analyze_schema = NULL;
          $$ = ast_make_node(AST_NODE_ANALYZE_BLOCK);
      }
    ;

analyze_item_list
    : analyze_item_list analyze_item
      {
          ast_free($2);
          $$ = $1;
      }
    | analyze_item
      {
          $$ = $1;
      }
    ;

/* operation column */
analyze_item
    : analyze_op TOKEN_IDENT TOKEN_SEMI
      {
          if (!current_analyze_schema) {
              semantic_error("internal error: no active analyze schema");
              YYERROR;
          }
          int column_index = schema_find_column(current_analyze_schema, $2);
          if (column_index < 0) {
              size_t len = strlen($2) + 64;
              char *msg = (char *)malloc(len);
              if (msg) {
                  snprintf(msg, len, "column '%s' not found in known schemas", $2);
                  semantic_error(msg);
                  free(msg);
              } else {
                  semantic_error("column not found in any schema");
              }
              YYERROR;
          }
          VariableType col_type = current_analyze_schema->column_types[column_index];
          int type_error = 0;
          switch ($1) {
          case ANALYZE_OP_MEAN:
          case ANALYZE_OP_MEDIAN:
          case ANALYZE_OP_STDDEV:
          case ANALYZE_OP_SUMMARY:
              if (!(col_type == VAR_INT || col_type == VAR_FLOAT)) {
                  type_error = 1;
              }
              break;
          case ANALYZE_OP_MIN:
          case ANALYZE_OP_MAX:
              /* min/max can operate on numeric columns */
              if (!(col_type == VAR_INT || col_type == VAR_FLOAT)) {
                  type_error = 1;
              }
              break;
          case ANALYZE_OP_HISTOGRAM:
              /* allow any type */
              break;
          }
          if (type_error) {
              size_t len = strlen($2) + 64;
              char *msg = (char *)malloc(len);
              if (msg) {
                  snprintf(msg, len, "analyze operation incompatible with column '%s'", $2);
                  semantic_error(msg);
                  free(msg);
              } else {
                  semantic_error("analyze operation incompatible with column");
              }
              YYERROR;
          }
          if (record_analyze_request(current_analyze_schema, $2, $1, col_type, column_index) != 0) {
              YYERROR;
          }
          $$ = ast_make_node(AST_NODE_ANALYZE_BLOCK);
      }
    ;

analyze_op
    : TOKEN_MEAN
      {
          $$ = ANALYZE_OP_MEAN;
      }
    | TOKEN_MEDIAN
      {
          $$ = ANALYZE_OP_MEDIAN;
      }
    | TOKEN_STDDEV
      {
          $$ = ANALYZE_OP_STDDEV;
      }
    | TOKEN_MIN
      {
          $$ = ANALYZE_OP_MIN;
      }
    | TOKEN_MAX
      {
          $$ = ANALYZE_OP_MAX;
      }
    | TOKEN_HISTOGRAM
      {
          $$ = ANALYZE_OP_HISTOGRAM;
      }
    | TOKEN_SUMMARY
      {
          $$ = ANALYZE_OP_SUMMARY;
      }
    ;

%%

static void yyerror(const char *msg) {
    fprintf(stderr, "Syntax error: %s\n", msg);
}

static void parser_log(const char *stage) {
    if (!g_emit_parser_logs) {
        return;
    }
    fprintf(stdout, "[parser] %s\n", stage);
}

static char *dup_text(const char *text) {
    if (!text) {
        return NULL;
    }
    size_t len = strlen(text);
    char *copy = (char *)malloc(len + 1);
    if (!copy) {
        return NULL;
    }
    memcpy(copy, text, len + 1);
    return copy;
}

static void make_c_identifier(const char *src, char *dst, size_t dst_size) {
    if (!dst || dst_size == 0) {
        return;
    }
    if (!src || src[0] == '\0') {
        src = "generated";
    }
    size_t di = 0;
    if (!(isalpha((unsigned char)src[0]) || src[0] == '_')) {
        if (di < dst_size - 1) {
            dst[di++] = '_';
        }
    }
    for (size_t i = 0; src[i] != '\0' && di < dst_size - 1; ++i) {
        unsigned char c = (unsigned char)src[i];
        if (!(isalnum(c) || c == '_')) {
            c = '_';
        }
        dst[di++] = (char)c;
    }
    dst[di] = '\0';
}

static const char *runtime_literal_for_type(VariableType type) {
    switch (type) {
    case VAR_INT:
    case VAR_BOOL:
        return "RT_INT";
    case VAR_FLOAT:
        return "RT_FLOAT";
    case VAR_STRING:
        return "RT_STRING";
    }
    return "RT_INT";
}

static const char *c_type_for_var(VariableType type) {
    switch (type) {
    case VAR_INT:
    case VAR_BOOL:
        return "long long";
    case VAR_FLOAT:
        return "double";
    case VAR_STRING:
        return "char *";
    }
    return "long long";
}

static int path_is_absolute(const char *path) {
    if (!path || path[0] == '\0') {
        return 0;
    }
    if (path[0] == '/' || path[0] == '\\') {
        return 1;
    }
    if (isalpha((unsigned char)path[0]) && path[1] == ':') {
        return 1;
    }
    return 0;
}

static const char *codegen_resolve_csv_path(const char *raw, char *buffer, size_t buffer_size) {
    if (!raw || !buffer || buffer_size == 0) {
        return NULL;
    }
    if (path_is_absolute(raw)) {
        strncpy(buffer, raw, buffer_size - 1);
        buffer[buffer_size - 1] = '\0';
        return buffer;
    }
    snprintf(buffer, buffer_size, "%s", raw);
    return buffer;
}

static CodeExpr *code_expr_alloc(CodeExprKind kind, VariableType type) {
    CodeExpr *expr = (CodeExpr *)calloc(1, sizeof(CodeExpr));
    if (!expr) {
        return NULL;
    }
    expr->kind = kind;
    expr->type = type;
    return expr;
}

static CodeExpr *code_expr_new_literal_int(long long value) {
    CodeExpr *expr = code_expr_alloc(CODE_EXPR_LITERAL_INT, VAR_INT);
    if (!expr) {
        return NULL;
    }
    expr->data.int_val = value;
    return expr;
}

static CodeExpr *code_expr_new_literal_float(double value) {
    CodeExpr *expr = code_expr_alloc(CODE_EXPR_LITERAL_FLOAT, VAR_FLOAT);
    if (!expr) {
        return NULL;
    }
    expr->data.float_val = value;
    return expr;
}

static CodeExpr *code_expr_new_literal_string(const char *value) {
    if (!value) {
        return NULL;
    }
    CodeExpr *expr = code_expr_alloc(CODE_EXPR_LITERAL_STRING, VAR_STRING);
    if (!expr) {
        return NULL;
    }
    expr->data.str_val = dup_text(value);
    if (!expr->data.str_val) {
        free(expr);
        return NULL;
    }
    return expr;
}

static CodeExpr *code_expr_new_column(const char *schema_name, const char *column_name, int column_index, VariableType type) {
    CodeExpr *expr = code_expr_alloc(CODE_EXPR_COLUMN, type);
    if (!expr) {
        return NULL;
    }
    expr->data.column.schema_name = dup_text(schema_name);
    expr->data.column.column_name = dup_text(column_name);
    expr->data.column.column_index = column_index;
    if (!expr->data.column.schema_name || !expr->data.column.column_name) {
        free(expr->data.column.schema_name);
        free(expr->data.column.column_name);
        free(expr);
        return NULL;
    }
    return expr;
}

static CodeExpr *code_expr_new_binary(const char *op, CodeExpr *lhs, CodeExpr *rhs, VariableType type) {
    if (!lhs || !rhs) {
        return NULL;
    }
    CodeExpr *expr = code_expr_alloc(CODE_EXPR_BINARY, type);
    if (!expr) {
        return NULL;
    }
    strncpy(expr->data.binary.op, op, sizeof(expr->data.binary.op) - 1);
    expr->data.binary.lhs = lhs;
    expr->data.binary.rhs = rhs;
    return expr;
}

static CodeExpr *code_expr_new_unary_neg(CodeExpr *child, VariableType type) {
    if (!child) {
        return NULL;
    }
    CodeExpr *expr = code_expr_alloc(CODE_EXPR_UNARY_NEG, type);
    if (!expr) {
        return NULL;
    }
    expr->data.unary.child = child;
    return expr;
}

static void free_code_expr(CodeExpr *expr) {
    if (!expr) {
        return;
    }
    switch (expr->kind) {
    case CODE_EXPR_LITERAL_INT:
    case CODE_EXPR_LITERAL_FLOAT:
        break;
    case CODE_EXPR_LITERAL_STRING:
        free(expr->data.str_val);
        break;
    case CODE_EXPR_COLUMN:
        free(expr->data.column.schema_name);
        free(expr->data.column.column_name);
        break;
    case CODE_EXPR_BINARY:
        free_code_expr(expr->data.binary.lhs);
        free_code_expr(expr->data.binary.rhs);
        break;
    case CODE_EXPR_UNARY_NEG:
        free_code_expr(expr->data.unary.child);
        break;
    }
    free(expr);
}

static int resolve_column_reference(const char *dataset, const char *column, const Schema **out_schema, int *out_index, VariableType *out_type) {
    const Schema *schema = NULL;
    int index = -1;
    if (dataset) {
        schema = globals_find_schema(&globals, dataset);
        if (!schema) {
            schema = globals_schema_for_source(&globals, dataset);
        }
        if (!schema && current_dataset_schema && strcmp(current_dataset_schema->schema_name, dataset) == 0) {
            schema = current_dataset_schema;
        }
        if (!schema) {
            return -1;
        }
        index = schema_find_column(schema, column);
    } else {
        if (current_dataset_schema) {
            index = schema_find_column(current_dataset_schema, column);
            if (index >= 0) {
                schema = current_dataset_schema;
            }
        }
        if (!schema) {
            for (int i = 0; i < globals.schema_count; ++i) {
                int candidate = schema_find_column(&globals.schemas[i], column);
                if (candidate >= 0) {
                    schema = &globals.schemas[i];
                    index = candidate;
                    break;
                }
            }
        }
        if (!schema) {
            return -1;
        }
    }
    if (index < 0) {
        return 0;
    }
    if (out_schema) {
        *out_schema = schema;
    }
    if (out_index) {
        *out_index = index;
    }
    if (out_type) {
        *out_type = schema->column_types[index];
    }
    return 1;
}

static int schema_num_computed_columns(const Schema *schema) {
    if (!schema) {
        return 0;
    }
    int count = 0;
    for (int i = 0; i < compute_request_count; ++i) {
        if (strcmp(compute_requests[i].schema_name, schema->schema_name) == 0) {
            ++count;
        }
    }
    return count;
}

static int schema_base_column_count(const Schema *schema) {
    if (!schema) {
        return 0;
    }
    int total = schema->last_index + 1;
    if (total < 0) {
        total = 0;
    }
    int computed = schema_num_computed_columns(schema);
    int base = total - computed;
    if (base < 0) {
        base = 0;
    }
    return base;
}

static void build_schema_table_map(SchemaTableMap *map) {
    if (!map) {
        return;
    }
    map->count = 0;
    for (int i = 0; i < globals.schema_count && map->count < SYMTABLE_MAX_SYMBOLS; ++i) {
        SchemaTableBinding *entry = &map->entries[map->count++];
        entry->schema = &globals.schemas[i];
        char ident[NAME_MAX_LENGTH * 2];
        make_c_identifier(entry->schema->schema_name, ident, sizeof(ident));
        snprintf(entry->table_ident, sizeof(entry->table_ident), "table_%s", ident);
    }
}

static const char *codegen_lookup_table_base(const char *schema_name, void *userdata) {
    SchemaTableMap *map = (SchemaTableMap *)userdata;
    if (!map || !schema_name) {
        return "table_unknown";
    }
    for (int i = 0; i < map->count; ++i) {
        if (strcmp(map->entries[i].schema->schema_name, schema_name) == 0) {
            return map->entries[i].table_ident;
        }
    }
    return "table_unknown";
}

static void emit_c_string_literal(FILE *out, const char *value) {
    if (!value) {
        value = "";
    }
    fputc('"', out);
    for (const unsigned char *p = (const unsigned char *)value; *p; ++p) {
        switch (*p) {
        case '\\': fputs("\\\\", out); break;
        case '"':  fputs("\\\"", out); break;
        case '\n': fputs("\\n", out); break;
        case '\r': fputs("\\r", out); break;
        case '\t': fputs("\\t", out); break;
        default:
            if (*p < 32 || *p > 126) {
                fprintf(out, "\\x%02X", *p);
            } else {
                fputc(*p, out);
            }
            break;
        }
    }
    fputc('"', out);
}

/* Retrouve, pour le schema actuellement itere (current_schema), l'offset de colonne
 * correspondant a une colonne declaree a l'origine dans origin_schema. Les tables
 * generees pour join/filter contiennent une copie complete des colonnes de leur(s)
 * schema(s) source(s) (rt_table_copy_row / rt_table_copy_row_into), donc toute colonne
 * reference doit etre lue depuis la table du dataset courant, a l'offset ou elle a ete
 * recopiee, et non depuis la table du schema d'origine (qui n'a pas le meme ordre de
 * lignes des qu'un join ou un filtre a deja ete applique). */
static int resolve_offset_in_schema(const char *current_schema, const char *origin_schema, int origin_index, int depth) {
    if (!current_schema || !origin_schema || depth > 64) {
        return -1;
    }
    if (strcmp(current_schema, origin_schema) == 0) {
        return origin_index;
    }
    for (int i = 0; i < join_request_count; ++i) {
        JoinRequest *req = &join_requests[i];
        if (strcmp(req->target_schema, current_schema) == 0) {
            int left_off = resolve_offset_in_schema(req->left_schema, origin_schema, origin_index, depth + 1);
            if (left_off >= 0) {
                return left_off;
            }
            int right_off = resolve_offset_in_schema(req->right_schema, origin_schema, origin_index, depth + 1);
            if (right_off >= 0) {
                return req->left_column_count + right_off;
            }
            return -1;
        }
    }
    for (int i = 0; i < filter_request_count; ++i) {
        FilterRequest *req = &filter_requests[i];
        if (strcmp(req->target_schema, current_schema) == 0) {
            return resolve_offset_in_schema(req->source_schema, origin_schema, origin_index, depth + 1);
        }
    }
    return -1;
}

static void code_expr_emit(const CodeExpr *expr, FILE *out, const char *row_var, const char *current_schema, const char *(*schema_to_table)(const char *, void *), void *userdata) {
    if (!expr) {
        fprintf(out, "0");
        return;
    }
    switch (expr->kind) {
    case CODE_EXPR_LITERAL_INT:
        fprintf(out, "%lld", expr->data.int_val);
        break;
    case CODE_EXPR_LITERAL_FLOAT:
        fprintf(out, "%.17g", expr->data.float_val);
        break;
    case CODE_EXPR_LITERAL_STRING:
        emit_c_string_literal(out, expr->data.str_val);
        break;
    case CODE_EXPR_COLUMN:
        {
            const char *table_schema = current_schema;
            int offset = resolve_offset_in_schema(current_schema, expr->data.column.schema_name, expr->data.column.column_index, 0);
            if (offset < 0) {
                /* Repli : schema non retrouve dans les requetes join/filter enregistrees
                 * (ex. reference directe a une source non transformee). Comportement
                 * historique conserve dans ce cas. */
                offset = expr->data.column.column_index;
                table_schema = expr->data.column.schema_name;
            }
            const char *table_ident = schema_to_table(table_schema, userdata);
            const char *ctype = c_type_for_var(expr->type);
            fprintf(out, "((%s *)%s.columns[%d])[%s]", ctype, table_ident, offset, row_var);
        }
        break;
    case CODE_EXPR_BINARY:
        if (expr->type == VAR_BOOL && is_equality_operator(expr->data.binary.op) &&
            expr->data.binary.lhs && expr->data.binary.rhs &&
            expr->data.binary.lhs->type == VAR_STRING &&
            expr->data.binary.rhs->type == VAR_STRING) {
            fprintf(out, "(");
            fprintf(out, "strcmp(");
            code_expr_emit(expr->data.binary.lhs, out, row_var, current_schema, schema_to_table, userdata);
            fprintf(out, ", ");
            code_expr_emit(expr->data.binary.rhs, out, row_var, current_schema, schema_to_table, userdata);
            if (strcmp(expr->data.binary.op, "==") == 0) {
                fprintf(out, ") == 0)");
            } else {
                fprintf(out, ") != 0)");
            }
        } else {
            fprintf(out, "(");
            code_expr_emit(expr->data.binary.lhs, out, row_var, current_schema, schema_to_table, userdata);
            fprintf(out, " %s ", expr->data.binary.op);
            code_expr_emit(expr->data.binary.rhs, out, row_var, current_schema, schema_to_table, userdata);
            fprintf(out, ")");
        }
        break;
    case CODE_EXPR_UNARY_NEG:
        fprintf(out, "(-(");
        code_expr_emit(expr->data.unary.child, out, row_var, current_schema, schema_to_table, userdata);
        fprintf(out, "))");
        break;
    }
}

static int is_equality_operator(const char *op) {
    return op && (strcmp(op, "==") == 0 || strcmp(op, "!=") == 0);
}

static int expr_is_numeric_type(VariableType type) {
    return type == VAR_INT || type == VAR_FLOAT;
}

static VariableType expr_promote_numeric(VariableType a, VariableType b) {
    return (a == VAR_FLOAT || b == VAR_FLOAT) ? VAR_FLOAT : VAR_INT;
}

static ExprInfo expr_make(ASTNode *node, VariableType type, CodeExpr *code) {
    ExprInfo info;
    info.node = node;
    info.type = type;
    info.code = code;
    info.valid = (node != NULL || code != NULL);
    return info;
}

static ExprInfo expr_error(void) {
    ExprInfo info;
    info.node = NULL;
    info.type = VAR_INT;
    info.valid = 0;
    info.code = NULL;
    return info;
}

static ExprInfo expr_handle_arithmetic(ExprInfo left, ExprInfo right, const char *op) {
    if (!left.valid || !right.valid) {
        return expr_error();
    }
    if (!expr_is_numeric_type(left.type) || !expr_is_numeric_type(right.type)) {
        size_t len = strlen(op) + 64;
        char *msg = (char *)malloc(len);
        if (msg) {
            snprintf(msg, len, "operator '%s' expects numeric operands", op);
            semantic_error(msg);
            free(msg);
        } else {
            semantic_error("arithmetic operator expects numeric operands");
        }
        return expr_error();
    }
    return expr_make(ast_make_node(AST_NODE_EXPRESSION), expr_promote_numeric(left.type, right.type), NULL);
}

static int types_compatible_for_equality(VariableType a, VariableType b) {
    if (a == b) {
        return 1;
    }
    if (expr_is_numeric_type(a) && expr_is_numeric_type(b)) {
        return 1;
    }
    return 0;
}

static ExprInfo expr_handle_comparison(ExprInfo left, ExprInfo right, const char *op, int ordered) {
    if (!left.valid || !right.valid) {
        return expr_error();
    }
    if (ordered) {
        if (!expr_is_numeric_type(left.type) || !expr_is_numeric_type(right.type)) {
            size_t len = strlen(op) + 64;
            char *msg = (char *)malloc(len);
            if (msg) {
                snprintf(msg, len, "operator '%s' expects numeric operands", op);
                semantic_error(msg);
                free(msg);
            } else {
                semantic_error("comparison operator expects numeric operands");
            }
            return expr_error();
        }
    } else {
        if (!types_compatible_for_equality(left.type, right.type)) {
            size_t len = strlen(op) + 64;
            char *msg = (char *)malloc(len);
            if (msg) {
                snprintf(msg, len, "operator '%s' applied to incompatible operands", op);
                semantic_error(msg);
                free(msg);
            } else {
                semantic_error("comparison operator applied to incompatible operands");
            }
            return expr_error();
        }
    }
    CodeExpr *lhs = left.code;
    CodeExpr *rhs = right.code;
    left.code = NULL;
    right.code = NULL;
    if (!lhs || !rhs) {
        free_code_expr(lhs);
        free_code_expr(rhs);
        semantic_error("internal error: missing operand code for comparison");
        return expr_error();
    }
    CodeExpr *code = code_expr_new_binary(op, lhs, rhs, VAR_BOOL);
    if (!code) {
        free_code_expr(lhs);
        free_code_expr(rhs);
        semantic_error("internal error: cannot allocate comparison expression");
        return expr_error();
    }
    return expr_make(ast_make_node(AST_NODE_CONDITION), VAR_BOOL, code);
}

static int join_types_compatible(VariableType a, VariableType b) {
    if (a == b) {
        return 1;
    }
    if (expr_is_numeric_type(a) && expr_is_numeric_type(b)) {
        return 1;
    }
    return 0;
}

static ExprInfo make_identifier_expr(const char *ident) {
    ExprInfo info = expr_error();
    const Schema *schema = NULL;
    int column_index = -1;
    VariableType t = VAR_INT;
    if (resolve_column_reference(NULL, ident, &schema, &column_index, &t)) {
        CodeExpr *code = code_expr_new_column(schema->schema_name, schema->column_names[column_index], column_index, t);
        if (!code) {
            semantic_error("internal error: unable to allocate identifier expression");
            return expr_error();
        }
        return expr_make(ast_make_node(AST_NODE_EXPRESSION), t, code);
    }
    size_t len = strlen(ident) + 64;
    char *msg = (char *)malloc(len);
    if (msg) {
        snprintf(msg, len, "identifier '%s' is not declared", ident);
        semantic_error(msg);
        free(msg);
    } else {
        semantic_error("identifier is not declared");
    }
    return info;
}

static ExprInfo make_qualified_expr(const char *dataset, const char *column) {
    ExprInfo info = expr_error();
    const Schema *schema = NULL;
    int column_index = -1;
    VariableType t = VAR_INT;
    int status = resolve_column_reference(dataset, column, &schema, &column_index, &t);
    if (status == 1) {
        CodeExpr *code = code_expr_new_column(schema->schema_name, schema->column_names[column_index], column_index, t);
        if (!code) {
            semantic_error("internal error: unable to allocate identifier expression");
            return expr_error();
        }
        return expr_make(ast_make_node(AST_NODE_EXPRESSION), t, code);
    }
    if (status < 0) {
        size_t len = strlen(dataset) + 64;
        char *msg = (char *)malloc(len);
        if (msg) {
            snprintf(msg, len, "dataset '%s' is not declared", dataset);
            semantic_error(msg);
            free(msg);
        } else {
            semantic_error("dataset is not declared");
        }
    } else {
        size_t len = strlen(column) + strlen(dataset) + 64;
        char *msg = (char *)malloc(len);
        if (msg) {
            snprintf(msg, len, "column '%s' not found in schema '%s'", column, dataset);
            semantic_error(msg);
            free(msg);
        } else {
            semantic_error("column not found in dataset");
        }
    }
    return info;
}

static int record_compute_request(const Schema *schema, const char *column_name, VariableType result_type, CodeExpr *expr) {
    if (!schema || !column_name || !expr) {
        return -1;
    }
    if (compute_request_count >= MAX_COMPUTE_REQUESTS) {
        semantic_error("too many compute statements");
        return -1;
    }
    int column_index = schema_find_column(schema, column_name);
    if (column_index < 0) {
        semantic_error("internal error: computed column not found in schema");
        return -1;
    }
    ComputeRequest *slot = &compute_requests[compute_request_count++];
    strncpy(slot->schema_name, schema->schema_name, sizeof(slot->schema_name) - 1);
    slot->schema_name[sizeof(slot->schema_name) - 1] = '\0';
    strncpy(slot->column_name, column_name, sizeof(slot->column_name) - 1);
    slot->column_name[sizeof(slot->column_name) - 1] = '\0';
    slot->result_type = result_type;
    slot->expr = expr;
    slot->column_index = column_index;
    return 0;
}

static void release_compute_requests(void) {
    for (int i = 0; i < compute_request_count; ++i) {
        free_code_expr(compute_requests[i].expr);
        compute_requests[i].expr = NULL;
    }
    compute_request_count = 0;
}

static int record_analyze_request(const Schema *schema, const char *column_name, AnalyzeOp op, VariableType column_type, int column_index) {
    if (!schema || !column_name) {
        return -1;
    }
    if (analyze_request_count >= MAX_ANALYZE_REQUESTS) {
        semantic_error("too many analyze statements");
        return -1;
    }
    AnalyzeRequest *slot = &analyze_requests[analyze_request_count++];
    strncpy(slot->schema_name, schema->schema_name, sizeof(slot->schema_name) - 1);
    slot->schema_name[sizeof(slot->schema_name) - 1] = '\0';
    strncpy(slot->column_name, column_name, sizeof(slot->column_name) - 1);
    slot->column_name[sizeof(slot->column_name) - 1] = '\0';
    slot->op = op;
    slot->column_type = column_type;
    slot->column_index = column_index;
    return 0;
}

static void release_analyze_requests(void) {
    analyze_request_count = 0;
}

static int record_filter_request(const char *target_schema, const char *source_schema, CodeExpr *condition) {
    if (!target_schema || !source_schema || !condition) {
        return -1;
    }
    if (filter_request_count >= MAX_FILTER_REQUESTS) {
        semantic_error("too many filter statements");
        return -1;
    }
    FilterRequest *slot = &filter_requests[filter_request_count++];
    strncpy(slot->target_schema, target_schema, sizeof(slot->target_schema) - 1);
    slot->target_schema[sizeof(slot->target_schema) - 1] = '\0';
    strncpy(slot->source_schema, source_schema, sizeof(slot->source_schema) - 1);
    slot->source_schema[sizeof(slot->source_schema) - 1] = '\0';
    slot->condition = condition;
    return 0;
}

static void release_filter_requests(void) {
    for (int i = 0; i < filter_request_count; ++i) {
        free_code_expr(filter_requests[i].condition);
        filter_requests[i].condition = NULL;
    }
    filter_request_count = 0;
}

static void release_join_requests(void) {
    join_request_count = 0;
}

static int record_join_request(const Schema *target, const Schema *left, int left_col_index, const Schema *right, int right_col_index, VariableType join_type) {
    if (!target || !left || !right) {
        return -1;
    }
    if (join_request_count >= MAX_JOIN_REQUESTS) {
        semantic_error("too many join statements");
        return -1;
    }
    JoinRequest *slot = &join_requests[join_request_count++];
    strncpy(slot->target_schema, target->schema_name, sizeof(slot->target_schema) - 1);
    slot->target_schema[sizeof(slot->target_schema) - 1] = '\0';
    strncpy(slot->left_schema, left->schema_name, sizeof(slot->left_schema) - 1);
    slot->left_schema[sizeof(slot->left_schema) - 1] = '\0';
    strncpy(slot->right_schema, right->schema_name, sizeof(slot->right_schema) - 1);
    slot->right_schema[sizeof(slot->right_schema) - 1] = '\0';
    slot->left_column_index = left_col_index;
    slot->right_column_index = right_col_index;
    slot->join_type = join_type;
    slot->left_column_count = left->last_index + 1;
    slot->right_column_count = right->last_index + 1;
    if (slot->left_column_count < 0) slot->left_column_count = 0;
    if (slot->right_column_count < 0) slot->right_column_count = 0;
    return 0;
}

static const char *analyze_label(AnalyzeOp op) {
    switch (op) {
    case ANALYZE_OP_MEAN: return "mean";
    case ANALYZE_OP_MEDIAN: return "median";
    case ANALYZE_OP_STDDEV: return "std_dev";
    case ANALYZE_OP_MIN: return "min";
    case ANALYZE_OP_MAX: return "max";
    case ANALYZE_OP_HISTOGRAM: return "histogram";
    case ANALYZE_OP_SUMMARY: return "summary";
    }
    return "analyze";
}

static void reset_codegen_state(void) {
    release_compute_requests();
    release_analyze_requests();
    release_filter_requests();
    release_join_requests();
}

static void semantic_error(const char *msg) {
    yyerror((char *)msg);
}

static char *unquote_string(const char *text) {
    if (!text) {
        return NULL;
    }
    size_t len = strlen(text);
    if (len >= 2 && text[0] == '"' && text[len - 1] == '"') {
        size_t n = len - 2;
        char *buf = (char *)malloc(n + 1);
        if (!buf) {
            return NULL;
        }
        memcpy(buf, text + 1, n);
        buf[n] = '\0';
        return buf;
    }
    return NULL;
}



static void print_usage(const char *prog) {
    fprintf(stderr, "Usage: %s [options] <source file>\n", prog);
    fprintf(stderr, "Options:\n");
    fprintf(stderr, "  -syntax-only    Run lexer/parser only (no output file, no traces)\n");
    fprintf(stderr, "  -tos            Dump symbol tables after parsing\n");
    fprintf(stderr, "  -version        Display version information and exit\n");
    fprintf(stderr, "  -o <file>       Write generated C output to <file> (default: afstat.c)\n");
}

static int write_output_stub(const char *path) {
    if (!path) {
        return 0;
    }
    FILE *out = fopen(path, "w");
    if (!out) {
        fprintf(stderr, "Could not open output file '%s': %s\n", path, strerror(errno));
        reset_codegen_state();
        return -1;
    }
    SchemaTableMap table_map;
    build_schema_table_map(&table_map);

    fprintf(out, "#include <stdio.h>\n");
    fprintf(out, "#include <stdlib.h>\n");
    fprintf(out, "#include <string.h>\n");
    fprintf(out, "#include \"runtime.h\"\n\n");

    for (int i = 0; i < table_map.count; ++i) {
        fprintf(out, "static RtTable %s = {0};\n", table_map.entries[i].table_ident);
    }

    fprintf(out, "\nint main(void) {\n");
    fprintf(out, "    int status = 0;\n");

    for (int i = 0; i < globals.association_count; ++i) {
        const Association *assoc = &globals.associations[i];
        const Schema *schema = globals_find_schema(&globals, assoc->schema_name);
        const Source *source = globals_find_source(&globals, assoc->source_name);
        if (!schema || !source) {
            continue;
        }
        int base_cols = schema_base_column_count(schema);
        if (base_cols <= 0) {
            continue;
        }
        const char *table_ident = codegen_lookup_table_base(schema->schema_name, &table_map);
        fprintf(out, "    const RtType %s_types[] = {", table_ident);
        for (int col = 0; col < base_cols; ++col) {
            if (col > 0) {
                fputs(", ", out);
            }
            fprintf(out, "%s", runtime_literal_for_type(schema->column_types[col]));
        }
        fprintf(out, "};\n");
        fprintf(out, "    const char *%s_names[] = {", table_ident);
        for (int col = 0; col < base_cols; ++col) {
            if (col > 0) {
                fputs(", ", out);
            }
            emit_c_string_literal(out, schema->column_names[col]);
        }
        fprintf(out, "};\n");

        char path_buf[512];
        const char *resolved = codegen_resolve_csv_path(source->source_path, path_buf, sizeof(path_buf));
        fprintf(out, "    if (rt_load_csv(\"%s\", %s_types, %s_names, %d, &%s) != 0) {\n",
                resolved ? resolved : source->source_path, table_ident, table_ident, base_cols, table_ident);
        fprintf(out, "        fprintf(stderr, \"Failed to load %%s from %%s\\n\", \"%s\", \"%s\");\n",
                schema->schema_name, source->source_name);
        fprintf(out, "        status = 1;\n");
        fprintf(out, "        goto cleanup;\n");
        fprintf(out, "    }\n\n");
    }

    /* Joins */
    for (int i = 0; i < join_request_count; ++i) {
        JoinRequest *req = &join_requests[i];
        const Schema *target_schema = globals_find_schema(&globals, req->target_schema);
        const Schema *left_schema = globals_find_schema(&globals, req->left_schema);
        const Schema *right_schema = globals_find_schema(&globals, req->right_schema);
        if (!target_schema || !left_schema || !right_schema) {
            continue;
        }
        int total_cols = target_schema->last_index + 1;
        if (total_cols < 0) total_cols = 0;
        const char *left_table = codegen_lookup_table_base(req->left_schema, &table_map);
        const char *right_table = codegen_lookup_table_base(req->right_schema, &table_map);
        const char *join_table = codegen_lookup_table_base(req->target_schema, &table_map);

        fprintf(out, "    {\n");
        fprintf(out, "        const RtType %s_types[] = {", join_table);
        for (int c = 0; c < total_cols; ++c) {
            if (c > 0) fputs(", ", out);
            fprintf(out, "%s", runtime_literal_for_type(target_schema->column_types[c]));
        }
        fprintf(out, "};\n");
        fprintf(out, "        if (rt_table_init_with_types(%s_types, %d, %s.rows * %s.rows, &%s) != 0) {\n", join_table, total_cols, left_table, right_table, join_table);
        fprintf(out, "            fprintf(stderr, \"Failed to init join %s\\n\");\n", req->target_schema);
        fprintf(out, "            status = 1;\n");
        fprintf(out, "            goto cleanup;\n");
        fprintf(out, "        }\n");
        fprintf(out, "        for (size_t r1 = 0; r1 < %s.rows; ++r1) {\n", left_table);
        fprintf(out, "            for (size_t r2 = 0; r2 < %s.rows; ++r2) {\n", right_table);
        fprintf(out, "                int match = 0;\n");
        switch (req->join_type) {
        case VAR_INT:
            fprintf(out, "                long long lv = ((long long *)%s.columns[%d])[r1];\n", left_table, req->left_column_index);
            fprintf(out, "                long long rv = ((long long *)%s.columns[%d])[r2];\n", right_table, req->right_column_index);
            fprintf(out, "                match = (lv == rv);\n");
            break;
        case VAR_FLOAT:
            fprintf(out, "                double lv = ((double *)%s.columns[%d])[r1];\n", left_table, req->left_column_index);
            fprintf(out, "                double rv = ((double *)%s.columns[%d])[r2];\n", right_table, req->right_column_index);
            fprintf(out, "                match = (lv == rv);\n");
            break;
        case VAR_STRING:
            fprintf(out, "                char *lv = ((char **)%s.columns[%d])[r1];\n", left_table, req->left_column_index);
            fprintf(out, "                char *rv = ((char **)%s.columns[%d])[r2];\n", right_table, req->right_column_index);
            fprintf(out, "                match = (lv && rv && strcmp(lv, rv) == 0);\n");
            break;
        case VAR_BOOL:
            fprintf(out, "                long long lv = ((long long *)%s.columns[%d])[r1];\n", left_table, req->left_column_index);
            fprintf(out, "                long long rv = ((long long *)%s.columns[%d])[r2];\n", right_table, req->right_column_index);
            fprintf(out, "                match = (lv == rv);\n");
            break;
        }
        fprintf(out, "                if (match) {\n");
        fprintf(out, "                    ssize_t new_row = rt_table_append_row(&%s);\n", join_table);
        fprintf(out, "                    if (new_row < 0) { status = 1; goto cleanup; }\n");
        fprintf(out, "                    if (rt_table_copy_row_into(&%s, new_row, &%s, r1, 0) != 0) { status = 1; goto cleanup; }\n", join_table, left_table);
        fprintf(out, "                    if (rt_table_copy_row_into(&%s, new_row, &%s, r2, %d) != 0) { status = 1; goto cleanup; }\n", join_table, right_table, req->left_column_count);
        fprintf(out, "                }\n");
        fprintf(out, "            }\n");
        fprintf(out, "        }\n");
        fprintf(out, "    }\n\n");
    }

    for (int i = 0; i < filter_request_count; ++i) {
        FilterRequest *req = &filter_requests[i];
        const char *src_table = codegen_lookup_table_base(req->source_schema, &table_map);
        const char *dst_table = codegen_lookup_table_base(req->target_schema, &table_map);
        fprintf(out, "    {\n");
        fprintf(out, "        if (rt_table_clone_structure(&%s, &%s, %s.rows) != 0) {\n", src_table, dst_table, src_table);
        fprintf(out, "            fprintf(stderr, \"Failed to prepare filter %s\\n\");\n", req->target_schema);
        fprintf(out, "            status = 1;\n");
        fprintf(out, "            goto cleanup;\n");
        fprintf(out, "        }\n");
        fprintf(out, "        for (size_t r = 0; r < %s.rows; ++r) {\n", src_table);
        fprintf(out, "            if (");
        code_expr_emit(req->condition, out, "r", req->source_schema, codegen_lookup_table_base, &table_map);
        fprintf(out, ") {\n");
        fprintf(out, "                ssize_t new_row = rt_table_append_row(&%s);\n", dst_table);
        fprintf(out, "                if (new_row < 0) {\n");
        fprintf(out, "                    fprintf(stderr, \"Failed to append row in filter %s\\n\");\n", req->target_schema);
        fprintf(out, "                    status = 1;\n");
        fprintf(out, "                    goto cleanup;\n");
        fprintf(out, "                }\n");
        fprintf(out, "                if (rt_table_copy_row(&%s, new_row, &%s, r) != 0) {\n", dst_table, src_table);
        fprintf(out, "                    fprintf(stderr, \"Failed to copy row in filter %s\\n\");\n", req->target_schema);
        fprintf(out, "                    status = 1;\n");
        fprintf(out, "                    goto cleanup;\n");
        fprintf(out, "                }\n");
        fprintf(out, "            }\n");
        fprintf(out, "        }\n");
        fprintf(out, "    }\n\n");
    }

    for (int i = 0; i < compute_request_count; ++i) {
        ComputeRequest *req = &compute_requests[i];
        const char *table_ident = codegen_lookup_table_base(req->schema_name, &table_map);
        const char *rt_type = runtime_literal_for_type(req->result_type);
        const char *ctype = c_type_for_var(req->result_type);
        fprintf(out, "    {\n");
        fprintf(out, "        int col_index = %d;\n", req->column_index);
        fprintf(out, "        if (col_index < 0 || (size_t)col_index >= %s.ncols || %s.types[col_index] != %s) {\n", table_ident, table_ident, rt_type);
        fprintf(out, "            col_index = rt_table_add_column(&%s, %s);\n", table_ident, rt_type);
        fprintf(out, "        }\n");
        fprintf(out, "        if (col_index < 0 || (size_t)col_index >= %s.ncols || %s.types[col_index] != %s) {\n", table_ident, table_ident, rt_type);
        fprintf(out, "            fprintf(stderr, \"Computed column %%s missing or type mismatch in schema %%s\\n\", \"%s\", \"%s\");\n",
                req->column_name, req->schema_name);
        fprintf(out, "            status = 1;\n");
        fprintf(out, "            goto cleanup;\n");
        fprintf(out, "        }\n");
        fprintf(out, "        %s *col_ptr = (%s *)%s.columns[col_index];\n", ctype, ctype, table_ident);
        fprintf(out, "        for (size_t r = 0; r < %s.rows; ++r) {\n", table_ident);
        fprintf(out, "            col_ptr[r] = ");
        code_expr_emit(req->expr, out, "r", req->schema_name, codegen_lookup_table_base, &table_map);
        fprintf(out, ";\n");
        fprintf(out, "        }\n");
        fprintf(out, "    }\n\n");
    }

    for (int i = 0; i < analyze_request_count; ++i) {
        AnalyzeRequest *req = &analyze_requests[i];
        const char *table_ident = codegen_lookup_table_base(req->schema_name, &table_map);
        fprintf(out, "    {\n");
        switch (req->op) {
        case ANALYZE_OP_MEAN:
        case ANALYZE_OP_MEDIAN:
        case ANALYZE_OP_STDDEV:
        case ANALYZE_OP_MIN:
        case ANALYZE_OP_MAX:
            {
                const char *fn = NULL;
                switch (req->op) {
                case ANALYZE_OP_MEAN: fn = "rt_mean"; break;
                case ANALYZE_OP_MEDIAN: fn = "rt_median"; break;
                case ANALYZE_OP_STDDEV: fn = "rt_stddev"; break;
                case ANALYZE_OP_MIN: fn = "rt_min_num"; break;
                case ANALYZE_OP_MAX: fn = "rt_max_num"; break;
                default: fn = "rt_mean"; break;
                }
                fprintf(out, "        int ok = 0;\n");
                fprintf(out, "        double value = %s(&%s, %d, &ok);\n", fn, table_ident, req->column_index);
                fprintf(out, "        if (ok) {\n");
                fprintf(out, "            printf(\"%s %s.%s = %%.6f\\n\", value);\n", analyze_label(req->op), req->schema_name, req->column_name);
                fprintf(out, "        } else {\n");
                fprintf(out, "            printf(\"%s %s.%s : colonne incompatible\\n\");\n", analyze_label(req->op), req->schema_name, req->column_name);
                fprintf(out, "        }\n");
            }
            break;
        case ANALYZE_OP_SUMMARY:
            fprintf(out, "        int ok_min = 0, ok_max = 0, ok_mean = 0, ok_std = 0;\n");
            fprintf(out, "        double min_v = rt_min_num(&%s, %d, &ok_min);\n", table_ident, req->column_index);
            fprintf(out, "        double max_v = rt_max_num(&%s, %d, &ok_max);\n", table_ident, req->column_index);
            fprintf(out, "        double mean_v = rt_mean(&%s, %d, &ok_mean);\n", table_ident, req->column_index);
            fprintf(out, "        double std_v = rt_stddev(&%s, %d, &ok_std);\n", table_ident, req->column_index);
            fprintf(out, "        if (ok_min && ok_max && ok_mean && ok_std) {\n");
            fprintf(out, "            printf(\"summary %s.%s:\\n\");\n", req->schema_name, req->column_name);
            fprintf(out, "            printf(\"  min: %%.6f\\n\", min_v);\n");
            fprintf(out, "            printf(\"  max: %%.6f\\n\", max_v);\n");
            fprintf(out, "            printf(\"  mean: %%.6f\\n\", mean_v);\n");
            fprintf(out, "            printf(\"  std_dev: %%.6f\\n\", std_v);\n");
            fprintf(out, "        } else {\n");
            fprintf(out, "            printf(\"summary %s.%s : colonne incompatible\\n\");\n", req->schema_name, req->column_name);
            fprintf(out, "        }\n");
            break;
        case ANALYZE_OP_HISTOGRAM:
            fprintf(out, "        char **values = NULL;\n");
            fprintf(out, "        size_t *counts = NULL;\n");
            fprintf(out, "        size_t buckets = rt_histogram_string(&%s, %d, &values, &counts);\n", table_ident, req->column_index);
            fprintf(out, "        printf(\"histogram %s.%s (%%zu categorie(s))\\n\", buckets);\n", req->schema_name, req->column_name);
            fprintf(out, "        for (size_t h = 0; h < buckets; ++h) {\n");
            fprintf(out, "            printf(\"  %%s : %%zu\\n\", values[h] ? values[h] : \"(null)\", counts[h]);\n");
            fprintf(out, "            free(values[h]);\n");
            fprintf(out, "        }\n");
            fprintf(out, "        free(values);\n");
            fprintf(out, "        free(counts);\n");
            break;
        }
        fprintf(out, "    }\n\n");
    }

    fprintf(out, "cleanup:\n");
    for (int i = 0; i < table_map.count; ++i) {
        fprintf(out, "    rt_free_table(&%s);\n", table_map.entries[i].table_ident);
    }
    fprintf(out, "    return status;\n");
    fprintf(out, "}\n");

    fclose(out);
    reset_codegen_state();
    return 0;
}

int main(int argc, char **argv) {
    const char *input_path = NULL;
    const char *output_path = NULL;
    int show_version = 0;

    for (int i = 1; i < argc; ++i) {
        const char *arg = argv[i];
        if (strcmp(arg, "-syntax-only") == 0) {
            g_emit_tokens = 0;
            g_emit_parser_logs = 0;
            g_syntax_only = 1;
        } else if (strcmp(arg, "-tos") == 0) {
            g_dump_symbol_table = 1;
        } else if (strcmp(arg, "-version") == 0) {
            show_version = 1;
        } else if (strcmp(arg, "-o") == 0) {
            if (i + 1 >= argc) {
                fprintf(stderr, "Option -o requires a file name\n");
                print_usage(argv[0]);
                return EXIT_FAILURE;
            }
            output_path = argv[++i];
        } else if (arg[0] == '-') {
            fprintf(stderr, "Unknown option: %s\n", arg);
            print_usage(argv[0]);
            return EXIT_FAILURE;
        } else {
            input_path = arg;
        }
    }

    if (show_version) {
        printf("%s\n", g_version_info);
        return EXIT_SUCCESS;
    }

    if (!input_path) {
        print_usage(argv[0]);
        return EXIT_FAILURE;
    }

    if (!output_path) {
        output_path = "afstat.c";
    }

    g_output_path = output_path;

    FILE *input = fopen(input_path, "r");
    if (!input) {
        fprintf(stderr, "Cannot open '%s': %s\n", input_path, strerror(errno));
        return EXIT_FAILURE;
    }

    yyin = input;

    globals_init(&globals);
    compute_request_count = 0;
    analyze_request_count = 0;
    filter_request_count = 0;

    int result = yyparse();

    fclose(input);

    if (result == 0) {
        parser_log("parse completed successfully");
        if (g_dump_symbol_table) {
            globals_dump(&globals, stdout);
        }
        if (!g_syntax_only) {
            if (write_output_stub(g_output_path) != 0) {
                reset_codegen_state();
                return EXIT_FAILURE;
            }
        } else {
            printf("Syntax OK\n");
            reset_codegen_state();
        }
    } else {
        reset_codegen_state();
    }

    return result == 0 ? EXIT_SUCCESS : EXIT_FAILURE;
}
