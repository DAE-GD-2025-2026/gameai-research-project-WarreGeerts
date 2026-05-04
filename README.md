# Research project: Wave Function Collapse
This project is a research project for the DAE course Game Ai (algorithms 2). I will research the topic "Wave Function Collapse" because I was already planning on trying on this new challenge and jumping into procedural generation.

## 1. Introduction
This project explores Wave Function Collapse (WFC), a constraint-based procedural generation algorithm inspired by quantum mechanics. Unlike traditional noise-based generation, WFC creates patterns that adhere to a strict set of predefined adjacency rules.

## 2. The Algorithm
Wave Function Collapse works by treating every cell in a grid as a "superposition" of all possible states (tiles). The process follows these core steps:

* **Initialization**: Every cell starts with all possible tiles enabled.
* **Observation**: Find the cell with the lowest entropy (the fewest remaining possibilities) and "collapse" it by choosing one state at random (weighted by frequency).
* **Propagation**: Communicate the consequences of that choice to neighboring cells, removing tiles that are no longer valid based on adjacency rules.
* **Iteration**: Repeat until all cells are collapsed or a contradiction (error) occurs.

### Key Concepts Researched
* **Shannon Entropy**: How the algorithm decides which cell to collapse next.
* **Adjacency Constraints**: Defining which tiles can sit next to each other (North, South, East, West).

## 3. Implementation Details
Input Method: Simple Tile Model

Language/Engine: Unity

<!--Optimization: [Mention any specific data structures used, like bitsets for tile possibilities or a stack-based propagation.]

How to Run
Open the project in [Software Version].

Press [Key] to generate a new map.

[Include any other specific controls].-->

## 4. Research Findings & Challenges
During the development, I focused on:

* **Contradiction Handling**: Discussing whether the algorithm restarts or attempts to backtrack when it hits a dead end.
* **Performance**: Observations on how the grid size (N x M) impacts the propagation time.
* **Heuristics**: Testing if picking cells with the lowest entropy significantly reduces the failure rate compared to random selection.

## 5. Visuals & Demos
[WIP]

## 6. Conclusion
[WIP]

## 7. Sources & References
* **Original WFC Repository**: [Maxim Gumin's WFC](https://github.com/mxgmn/WaveFunctionCollapse)

* **Technical Explanation**: [Robert Heaton - The Wavefunction Collapse Algorithm explained](https://robertheaton.com/2018/12/17/wavefunction-collapse-algorithm/)
