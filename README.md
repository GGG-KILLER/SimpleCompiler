# SimpleCompiler
A not-so-simple optimizing compiler that aims to compile Lua (or other sources, given a proper frontend) into CIL (or other targets, given a proper backend).

This project uses Loretta (https://github.com/LorettaDevs/Loretta) as the parser (and syntax validator) and **PERFORMS ABSOLUTELY NO SEMANTIC VALIDATION**.

Current status:

- [x] Lowering of Lua into the IR.
- [ ] IR SSA rewriting.
- [ ] IR Optimizations:
    - [ ] Inlining.
    - [ ] Constant folding.
    - [ ] Invariant extraction.
    - [ ] Auto vectorization.
    - [ ] Dead code elimination.
- [ ] CIL emitting.
- [ ] PDB generation.

## References

For the IR, I've used the LLVM IR as inspiration.

For the SSA implementation, I've read the SSA Book: https://pfalcon.github.io/ssabook/latest/book.pdf