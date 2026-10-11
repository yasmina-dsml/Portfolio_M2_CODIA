#ifndef AST_H
#define AST_H

/* Minimal AST scaffolding while semantic actions are being wired. */

#include <stddef.h>

typedef enum {
    AST_NODE_NOP = 0,
    AST_NODE_PROGRAM,
    AST_NODE_SOURCE_DECL,
    AST_NODE_SCHEMA_DECL,
    AST_NODE_ASSOCIATE_DECL,
    AST_NODE_JOIN_STMT,
    AST_NODE_FILTER_STMT,
    AST_NODE_COMPUTE_STMT,
    AST_NODE_ANALYZE_BLOCK,
    AST_NODE_EXPRESSION,
    AST_NODE_CONDITION
} ASTNodeKind;

typedef struct ASTNode {
    ASTNodeKind kind;
} ASTNode;

ASTNode *ast_make_node(ASTNodeKind kind);
void ast_free(ASTNode *node);
void ast_print(const ASTNode *node, size_t indent);

#endif
