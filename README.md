# SimpleCompiler
A not-so-simple optimizing compiler that aims to compile Lua (or other sources, given a proper frontend) into CIL (or other targets, given a proper backend).

This project uses Loretta (https://github.com/LorettaDevs/Loretta) as the parser (and syntax validator) and **PERFORMS ABSOLUTELY NO SEMANTIC VALIDATION**.

Current status:

- [ ] Lowering of Lua into the IR:
    - [x] Basic literals (boolean, int, double, string, hash string).
    - [x] Unary expressions.
    - [x] Binary expressions.
    - [x] Function calls.
    - [ ] Tables.
    - [x] If statements.
    - [ ] While loops.
    - [ ] Numeric for loops.
    - [ ] Iterator for loops.
    - [ ] Local function declarations.
    - [ ] Function declarations.
- [x] IR SSA rewriting.
- [ ] IR Optimizations:
    - [x] Variable inlining.
    - [x] Constant folding.
    - [ ] Invariant extraction.
    - [ ] Auto vectorization.
    - [x] Dead code elimination.
    - [ ] Value numbering.
    - [ ] Inter-procedural analysis.
    - [ ] Function inlining.
- [ ] CIL emitting.
- [ ] PDB generation.

## References

For the IR, I've used the LLVM IR as inspiration.

For the SSA implementation, I've read the SSA Book: https://pfalcon.github.io/ssabook/latest/book.pdf