BISON  = bison
FLEX   = flex
CC     = gcc
CFLAGS = -Wall -Wextra -std=c11
RM     = rm -f

TARGET = afstat_parser
OBJS   = parser.tab.o lex.yy.o ast.o symtable.o runtime.o

TESTS := $(wildcard exemples/prog*.afs)

BUILD_DIR := build
GEN_DIR   := $(BUILD_DIR)/generated
BIN_DIR   := $(BUILD_DIR)/bin
EXEMPLES_DIR := exemples
TEST_DIR := test
NEG_DIR := $(TEST_DIR)/negative
POS_DIR := $(TEST_DIR)/positive
NEG_TESTS := $(wildcard $(NEG_DIR)/*.afs)
POS_TESTS := $(wildcard $(POS_DIR)/*.afs)

# Default target: build the combined lexer+parser 
all: $(TARGET)

parser.tab.c parser.tab.h: parser.y
	$(BISON) -d parser.y

lex.yy.c: lexer.l parser.tab.h
	$(FLEX) -o lex.yy.c lexer.l

parser.tab.o: parser.tab.c
	$(CC) $(CFLAGS) -c parser.tab.c

lex.yy.o: lex.yy.c parser.tab.h
	$(CC) $(CFLAGS) -c lex.yy.c

ast.o: ast.c ast.h
	$(CC) $(CFLAGS) -c ast.c

symtable.o: symtable.c symtable.h
	$(CC) $(CFLAGS) -c symtable.c

runtime.o: runtime.c runtime.h
	$(CC) $(CFLAGS) -c runtime.c

$(TARGET): $(OBJS)
	$(CC) $(CFLAGS) -o $(TARGET) $(OBJS) -lfl -lm

$(GEN_DIR) $(BIN_DIR):
	mkdir -p $@

clean:
	$(RM) $(TARGET) $(OBJS)
	$(RM) parser.tab.c parser.tab.h parser.output
	$(RM) lex.yy.c
	$(RM) lexer
	$(RM) *.o
	$(RM) afstat.c
	rm -rf $(BUILD_DIR)

# Simple parser smoke tests (just parse no code generation)
test: $(TARGET)
ifeq ($(strip $(TESTS)),)
	@echo "No test programs found in exemples/."
else
	@set -e; \
	for f in $(TESTS); do \
		echo "==> $$f"; \
		./$(TARGET) "$$f" >/dev/null; \
	done; \
	echo "All parser smoke tests passed.";
endif

# Parse -> generate C -> compile generated C (optional: RUN=1 to execute binaries to test code generation)
# Note: programs reference CSVs as "data/<file>.csv", so execution runs from $(EXEMPLES_DIR)
test_codegen: $(TARGET) | $(GEN_DIR) $(BIN_DIR)
ifeq ($(strip $(TESTS)),)
	@echo "No test programs found in exemples/."
else
	@set -e; \
	for f in $(TESTS); do \
		base=$$(basename "$$f" .afs); \
		cfile="$(GEN_DIR)/$$base.c"; \
		bin="$(BIN_DIR)/$$base"; \
		echo "==> $$f"; \
		./$(TARGET) -o "$$cfile" "$$f" >/dev/null; \
		$(CC) $(CFLAGS) -I. "$$cfile" runtime.o -o "$$bin" -lm; \
		if [ "$(RUN)" = "1" ]; then \
			( cd $(EXEMPLES_DIR) && "../$$bin" ); \
		fi; \
	done; \
	echo "All codegen+compile tests passed.";
endif

.PHONY: test_bad_schema
test_bad_schema: $(TARGET) runtime.o | $(GEN_DIR) $(BIN_DIR)
	@src=$(NEG_DIR)/bad_schema.afs; \
	cfile=$(GEN_DIR)/bad_schema.c; \
	bin=$(BIN_DIR)/bad_schema; \
	echo "==> $$src"; \
	./$(TARGET) -o "$$cfile" "$$src"; \
	$(CC) $(CFLAGS) -I. "$$cfile" runtime.o -lm -o "$$bin"; \
	"$$bin" || true

.PHONY: test_negative
test_negative: $(TARGET) runtime.o | $(GEN_DIR) $(BIN_DIR)
	@echo "Running negative tests (expected failures)"; \
	for f in $(NEG_TESTS); do \
		echo "==> $$f"; \
		case "$$f" in \
			test/bad_schema.afs) \
				cfile="$(GEN_DIR)/$$(basename $$f .afs).c"; \
				bin="$(BIN_DIR)/$$(basename $$f .afs)"; \
				./$(TARGET) -o "$$cfile" "$$f" >/tmp/neg_afstat.log 2>&1 && $(CC) $(CFLAGS) -I. "$$cfile" runtime.o -lm -o "$$bin" && "$$bin" || true; \
				cat /tmp/neg_afstat.log; \
				;; \
			*) \
				./$(TARGET) "$$f" || true; \
				;; \
		esac; \
		echo "----"; \
	done; \
	echo "Negative tests completed (errors above are expected)."

.PHONY: test_lexical_invalid test_semantic_unknown_column test_semantic_join_mismatch test_expr_type_mismatch test_undefined_identifier
test_lexical_invalid: $(TARGET)
	@echo "==> $(NEG_DIR)/lexical_invalid.afs"; \
	./$(TARGET) $(NEG_DIR)/lexical_invalid.afs || true

test_semantic_unknown_column: $(TARGET)
	@echo "==> $(NEG_DIR)/semantic_unknown_column.afs"; \
	./$(TARGET) $(NEG_DIR)/semantic_unknown_column.afs || true

test_semantic_join_mismatch: $(TARGET)
	@echo "==> $(NEG_DIR)/semantic_join_mismatch.afs"; \
	./$(TARGET) $(NEG_DIR)/semantic_join_mismatch.afs || true

test_syntax_error: $(TARGET)
	@echo "==> $(NEG_DIR)/syntax_error.afs"; \
	./$(TARGET) $(NEG_DIR)/syntax_error.afs || true

test_analyze_type_mismatch: $(TARGET)
	@echo "==> $(NEG_DIR)/analyze_type_mismatch.afs"; \
	./$(TARGET) $(NEG_DIR)/analyze_type_mismatch.afs || true

test_expr_type_mismatch: $(TARGET)
	@echo "==> $(NEG_DIR)/expr_type_mismatch.afs"; \
	./$(TARGET) $(NEG_DIR)/expr_type_mismatch.afs || true

test_undefined_identifier: $(TARGET)
	@echo "==> $(NEG_DIR)/undefined_identifier.afs"; \
	./$(TARGET) $(NEG_DIR)/undefined_identifier.afs || true

test_schema_type_mismatch: $(TARGET) runtime.o | $(GEN_DIR) $(BIN_DIR)
	@src=$(NEG_DIR)/schema_type_mismatch.afs; \
	cfile=$(GEN_DIR)/schema_type_mismatch.c; \
	bin=$(BIN_DIR)/schema_type_mismatch; \
	echo "==> $$src"; \
	./$(TARGET) -o "$$cfile" "$$src"; \
	$(CC) $(CFLAGS) -I. "$$cfile" runtime.o -lm -o "$$bin"; \
	"$$bin" || true

.PHONY: test_positive
test_positive: $(TARGET) runtime.o | $(GEN_DIR) $(BIN_DIR)
	@echo "Running positive tests"; \
	for f in $(POS_TESTS); do \
		base=$$(basename $$f .afs); \
		cfile="$(GEN_DIR)/pos_$${base}.c"; \
		bin="$(BIN_DIR)/pos_$${base}"; \
		echo "==> $$f"; \
		./$(TARGET) -o "$$cfile" "$$f" >/dev/null; \
		$(CC) $(CFLAGS) -I. "$$cfile" runtime.o -lm -o "$$bin"; \
		"$$bin"; \
		echo "----"; \
	done; \
	echo "Positive tests completed."

.PHONY: test_positive_compute test_positive_filter test_positive_histogram test_positive_join
test_positive_compute: $(TARGET) runtime.o | $(GEN_DIR) $(BIN_DIR)
	@src=$(POS_DIR)/compute.afs; \
	cfile=$(GEN_DIR)/pos_compute.c; \
	bin=$(BIN_DIR)/pos_compute; \
	echo "==> $$src"; \
	./$(TARGET) -o "$$cfile" "$$src" >/dev/null; \
	$(CC) $(CFLAGS) -I. "$$cfile" runtime.o -lm -o "$$bin"; \
	"$$bin"

test_positive_filter: $(TARGET) runtime.o | $(GEN_DIR) $(BIN_DIR)
	@src=$(POS_DIR)/filter.afs; \
	cfile=$(GEN_DIR)/pos_filter.c; \
	bin=$(BIN_DIR)/pos_filter; \
	echo "==> $$src"; \
	./$(TARGET) -o "$$cfile" "$$src" >/dev/null; \
	$(CC) $(CFLAGS) -I. "$$cfile" runtime.o -lm -o "$$bin"; \
	"$$bin"

test_positive_histogram: $(TARGET) runtime.o | $(GEN_DIR) $(BIN_DIR)
	@src=$(POS_DIR)/histogram.afs; \
	cfile=$(GEN_DIR)/pos_histogram.c; \
	bin=$(BIN_DIR)/pos_histogram; \
	echo "==> $$src"; \
	./$(TARGET) -o "$$cfile" "$$src" >/dev/null; \
	$(CC) $(CFLAGS) -I. "$$cfile" runtime.o -lm -o "$$bin"; \
	"$$bin"

test_positive_join: $(TARGET) runtime.o | $(GEN_DIR) $(BIN_DIR)
	@src=$(POS_DIR)/join.afs; \
	cfile=$(GEN_DIR)/pos_join.c; \
	bin=$(BIN_DIR)/pos_join; \
	echo "==> $$src"; \
	./$(TARGET) -o "$$cfile" "$$src" >/dev/null; \
	$(CC) $(CFLAGS) -I. "$$cfile" runtime.o -lm -o "$$bin"; \
	"$$bin"

.PHONY: test_positive_simple test_positive_join_compute test_positive_filter_string
test_positive_simple: $(TARGET) runtime.o | $(GEN_DIR) $(BIN_DIR)
	@src=$(POS_DIR)/simple.afs; \
	cfile=$(GEN_DIR)/pos_simple.c; \
	bin=$(BIN_DIR)/pos_simple; \
	echo "==> $$src"; \
	./$(TARGET) -o "$$cfile" "$$src" >/dev/null; \
	$(CC) $(CFLAGS) -I. "$$cfile" runtime.o -lm -o "$$bin"; \
	"$$bin"

test_positive_join_compute: $(TARGET) runtime.o | $(GEN_DIR) $(BIN_DIR)
	@src=$(POS_DIR)/join_compute.afs; \
	cfile=$(GEN_DIR)/pos_join_compute.c; \
	bin=$(BIN_DIR)/pos_join_compute; \
	echo "==> $$src"; \
	./$(TARGET) -o "$$cfile" "$$src" >/dev/null; \
	$(CC) $(CFLAGS) -I. "$$cfile" runtime.o -lm -o "$$bin"; \
	"$$bin"

test_positive_filter_string: $(TARGET) runtime.o | $(GEN_DIR) $(BIN_DIR)
	@src=$(POS_DIR)/filter_string.afs; \
	cfile=$(GEN_DIR)/pos_filter_string.c; \
	bin=$(BIN_DIR)/pos_filter_string; \
	echo "==> $$src"; \
	./$(TARGET) -o "$$cfile" "$$src" >/dev/null; \
	$(CC) $(CFLAGS) -I. "$$cfile" runtime.o -lm -o "$$bin"; \
	"$$bin"

.PHONY: all clean test test_codegen
