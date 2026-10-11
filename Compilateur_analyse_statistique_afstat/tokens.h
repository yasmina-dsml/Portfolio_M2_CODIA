#ifndef TOKENS_H
#define TOKENS_H

/* Debug helper: maps token ids to readable names. */
typedef int TokenType;

static const char *token_names[] = {
    "SOURCE","SCHEMA","ASSOCIATE","WITH","COMPUTE","ANALYZE","JOIN","INNER","FILTER","WHERE",
    "MEAN","MEDIAN","STDDEV","MIN","MAX","HISTOGRAM","SUMMARY",
    "INTEGER","FLOAT","STRING",
    "LBRACE","RBRACE","SEMI","DOT","ASSIGN","EQ","NEQ","LT","GT","LE","GE",
    "PLUS","MINUS","MUL","DIV","LPAREN","RPAREN",
    "INT_LIT","FLOAT_LIT","STRING_LIT","IDENT"
};

#endif
