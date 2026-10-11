%{
#define _POSIX_C_SOURCE 200112L
/* Flex scanner that recognises every AFSTAT lexical construct. */
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <unistd.h>
#include "ast.h"
#include "tokens.h"
#include "symtable.h"
#include "parser.tab.h"

extern FILE *yyin;
extern int fileno(FILE *);
extern int g_emit_tokens;

static char *dup_lexeme(const char *text) {
    size_t len = strlen(text);
    char *copy = (char *)malloc(len + 1);
    if (!copy) {
        return NULL;
    }
    memcpy(copy, text, len + 1);
    return copy;
}

/* Temporary helper to expose the token stream during bring-up. */
static void print_token(TokenType token, const char *lexeme) {

    if (!g_emit_tokens) {
        return;
    }

    if (token >= TOKEN_SOURCE)
        printf("%-12s : %s\n", token_names[token-256], lexeme);
    else
        printf("'%c'          : %s\n", token, lexeme);
}

%}

/* Tell Flex not to emit fallback routines the project does not use. */
%option nodefault
%option noinput
%option nounput

%%

"source"      { print_token(TOKEN_SOURCE, yytext); return TOKEN_SOURCE; }
"schema"      { print_token(TOKEN_SCHEMA, yytext); return TOKEN_SCHEMA; }
"associate"   { print_token(TOKEN_ASSOCIATE, yytext); return TOKEN_ASSOCIATE; }
"with"        { print_token(TOKEN_WITH, yytext); return TOKEN_WITH; }
"compute"     { print_token(TOKEN_COMPUTE, yytext); return TOKEN_COMPUTE; }
"analyze"     { print_token(TOKEN_ANALYZE, yytext); return TOKEN_ANALYZE; }
"join"        { print_token(TOKEN_JOIN, yytext); return TOKEN_JOIN; }
"inner"       { print_token(TOKEN_INNER, yytext); return TOKEN_INNER; }
"filter"      { print_token(TOKEN_FILTER, yytext); return TOKEN_FILTER; }
"where"       { print_token(TOKEN_WHERE, yytext); return TOKEN_WHERE; }
"mean"        { print_token(TOKEN_MEAN, yytext); return TOKEN_MEAN; }
"median"      { print_token(TOKEN_MEDIAN, yytext); return TOKEN_MEDIAN; }
"std_dev"     { print_token(TOKEN_STDDEV, yytext); return TOKEN_STDDEV; }
"min"         { print_token(TOKEN_MIN, yytext); return TOKEN_MIN; }
"max"         { print_token(TOKEN_MAX, yytext); return TOKEN_MAX; }
"histogram"   { print_token(TOKEN_HISTOGRAM, yytext); return TOKEN_HISTOGRAM; }
"summary"     { print_token(TOKEN_SUMMARY, yytext); return TOKEN_SUMMARY; }
"integer"     { print_token(TOKEN_INTEGER, yytext); return TOKEN_INTEGER; }
"float"       { print_token(TOKEN_FLOAT, yytext); return TOKEN_FLOAT; }
"string"      { print_token(TOKEN_STRING, yytext); return TOKEN_STRING; }

"=="          { print_token(TOKEN_EQ, yytext); return TOKEN_EQ; }
"!="          { print_token(TOKEN_NEQ, yytext); return TOKEN_NEQ; }
"<="          { print_token(TOKEN_LE, yytext); return TOKEN_LE; }
">="          { print_token(TOKEN_GE, yytext); return TOKEN_GE; }
"="           { print_token(TOKEN_ASSIGN, yytext); return TOKEN_ASSIGN; }
"<"           { print_token(TOKEN_LT, yytext); return TOKEN_LT; }
">"           { print_token(TOKEN_GT, yytext); return TOKEN_GT; }

"{"           { print_token(TOKEN_LBRACE, yytext); return TOKEN_LBRACE; }
"}"           { print_token(TOKEN_RBRACE, yytext); return TOKEN_RBRACE; }
";"           { print_token(TOKEN_SEMI, yytext); return TOKEN_SEMI; }
"."           { print_token(TOKEN_DOT, yytext); return TOKEN_DOT; }
"("           { print_token(TOKEN_LPAREN, yytext); return TOKEN_LPAREN; }
")"           { print_token(TOKEN_RPAREN, yytext); return TOKEN_RPAREN; }
"+"           { print_token(TOKEN_PLUS, yytext); return TOKEN_PLUS; }
"-"           { print_token(TOKEN_MINUS, yytext); return TOKEN_MINUS; }
"*"           { print_token(TOKEN_MUL, yytext); return TOKEN_MUL; }
"/"           { print_token(TOKEN_DIV, yytext); return TOKEN_DIV; }

[0-9]+"."[0-9]+    { print_token(TOKEN_FLOAT_LIT, yytext); yylval.float_val = strtod(yytext, NULL); return TOKEN_FLOAT_LIT; }
[0-9]+             { print_token(TOKEN_INT_LIT, yytext); yylval.int_val = strtoll(yytext, NULL, 10); return TOKEN_INT_LIT; }
\"([^\\"]|\\.)*\" { print_token(TOKEN_STRING_LIT, yytext); yylval.str = dup_lexeme(yytext); return TOKEN_STRING_LIT; }

[A-Za-z_][A-Za-z0-9_]* { print_token(TOKEN_IDENT, yytext); yylval.str = dup_lexeme(yytext); return TOKEN_IDENT; }

[ \t\r\n]+      ;  // skip whitespace
"//".*            ;  // skip comment

.                 { fprintf(stderr, "Lexical error: unexpected character '%s'\n", yytext); return YYUNDEF; }
<<EOF>> { return 0; }

%%

int yywrap(void) { return 1; }
