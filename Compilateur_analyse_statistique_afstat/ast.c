#include "ast.h"
#include <stdlib.h>
#include <stdio.h>

/* Allocate a single node AST placeholder */
ASTNode *ast_make_node(ASTNodeKind kind) {
    ASTNode *node = (ASTNode *)malloc(sizeof(ASTNode));
    if (!node) {
        return NULL;
    }
    node->kind = kind;
    return node;
}

/* Free helper kept separate in case nodes grow extra payload later */
void ast_free(ASTNode *node) {
    free(node);
}

void ast_print(const ASTNode *node, size_t indent) {
    if (!node) {
        return;
    }

    for (size_t i = 0; i < indent; ++i) {
        printf("  ");
    }

    switch (node->kind) {
    case AST_NODE_NOP:
        printf("NOP\n");
        break;
    case AST_NODE_PROGRAM:
        printf("PROGRAM\n");
        break;
    case AST_NODE_SOURCE_DECL:
        printf("SOURCE_DECL\n");
        break;
    case AST_NODE_SCHEMA_DECL:
        printf("SCHEMA_DECL\n");
        break;
    case AST_NODE_ASSOCIATE_DECL:
        printf("ASSOCIATE_DECL\n");
        break;
    case AST_NODE_JOIN_STMT:
        printf("JOIN_STMT\n");
        break;
    case AST_NODE_FILTER_STMT:
        printf("FILTER_STMT\n");
        break;
    case AST_NODE_COMPUTE_STMT:
        printf("COMPUTE_STMT\n");
        break;
    case AST_NODE_ANALYZE_BLOCK:
        printf("ANALYZE_BLOCK\n");
        break;
    case AST_NODE_EXPRESSION:
        printf("EXPRESSION\n");
        break;
    case AST_NODE_CONDITION:
        printf("CONDITION\n");
        break;
    default:
        printf("UNKNOWN NODE KIND\n");
        break;
    }
}
