# Research Project: Wave Function Collapse
**Course:** Game AI (Algorithms 2)  
**Institution:** Digital Arts and Entertainment (DAE), Howest  
**Author:** Warre Geerts  
**Academic Year:** 2025 - 2026  

---

## 1. Introduction
This research project explores the design, structural mechanics, and performance characteristics of the **Wave Function Collapse (WFC)** algorithm. Originally developed by Maxim Gumin, WFC is a constraint-based procedural generation method rooted in ideas inspired by quantum mechanics. Specifically, the concepts of superposition, entropy, and wave function collapse.

Traditional procedural content generation (PCG) workflows frequently rely on noise maps (such as Perlin or Simplex noise) combined with height-threshold filtering, or structural generative grammars (such as L-systems). While these methods excel at creating continuous landscapes or organic branching structures, they struggle when bound by explicit, rigid topological constraints. For instance, generating a structured urban environment, a subterranean dungeon, or a complex micro-tile map requires that certain game pieces connect flawlessly without visual or logical gaps.

WFC resolves this bottleneck by turning the generation problem upside down. Instead of sequentially building assets and hoping they fit, WFC starts with an open canvas where *every possibility coexists simultaneously*. It then systematically whittles down those possibilities until a singular, structurally valid state remains. This project investigates how changing constraint paradigms, adjusting grid volume sizes, and varying spatial dimensions (height profiles) impact overall execution times within a custom Unity implementation.

---

## 2. The Algorithm
The core philosophy of Wave Function Collapse is that an ungenerated grid cell does not possess a single, unknown identity; rather, it exists in a state of **superposition**, containing all potential tiles simultaneously. The algorithm operates by iteratively narrowing down these potential states until the wave function collapses into a concrete reality.

### The Generative Loop
The execution pipeline of WFC follows a strict, four-part cyclic state machine:

1. **Initialization**: The generation field is allocated as an $N \times M$ or $N \times Y \times M$ matrix. Every cell within this matrix is initialized to hold a full array or bitmask of all possible tile types defined in the tileset. At this moment, systemic entropy is at its absolute maximum.
2. **Observation**: The system scans the grid to locate the specific cell or group of cells possessing the *lowest non-zero Shannon entropy*. If multiple cells share the identical minimum entropy value, a random choice is made to break the tie. Once selected, this cell is forced to "collapse" into a single definitive state. This state selection is chosen at random from the cell's remaining valid possibilities, weighted by the natural frequency coefficients defined in the input model.
3. **Propagation**: The newly collapsed cell sends out a structural ripple. Its state change restricts what states its immediate neighbors can legally take based on predefined adjacency matrices. If a neighbor’s list of valid states is reduced, that neighbor in turn forces updates onto its own adjacent cells. This cascading wave continues until no further tiles can be legally eliminated from the grid.
4. **Iteration & Resolution**: The cycle loops back to the Observation phase. This continues uninterrupted until one of two termination states is reached:
    * **Success**: Every cell contains exactly one tile, resulting in a fully realized, valid procedural layout.
    * **Contradiction**: A propagation wave strips a cell of *all* possible tiles, leaving it with zero valid states. 

### Key Concepts Researched

#### Shannon Entropy
To make smart, stable generations, WFC cannot pick cells at random. It relies on **Shannon Entropy** to locate the most restricted areas of the map. Mathematically, the entropy $H$ of a discrete cell is computed using the frequencies of its remaining valid tiles:

$$H = \log_2(\sum_{i \in S} w_i) - \frac{\sum_{i \in S} w_i \log_2(w_i)}{\sum_{i \in S} w_i}$$

Where $S$ represents the set of remaining allowed tile indices within the cell, and $w_i$ represents the weight or global frequency coefficient of tile $i$. Choosing the cell with the lowest entropy ensures the algorithm resolves highly constrained zones first, significantly decreasing the likelihood of generating a contradiction later in the run.

#### Adjacency Constraints
Adjacency constraints form the systemic ruleset of the generation. For a standard 2D grid, these rules specify exactly which tiles can legally stand side-by-side across four primary directional vectors: **North (+Y), South (-Y), East (+X), and West (-X)**. In 3D environments, this expands to include vertical connections: **Up (+Z) and Down (-Z)**. If Tile A is designated to connect to Tile B on its eastern face, then Tile B must inherently accept Tile A on its western face.

---

## 3. Implementation Details
This project implements the **Simple Tile Model** within the Unity game engine. The architecture prioritizes low-overhead data manipulation to keep propagation times minimal.

* **Input Method**: Simple Tile Model. Tilesets are pre-authored as discrete mesh components with structural connection sockets mapped to each face.
* **Language/Engine**: C# within the Unity Engine.
* **Optimization Vectors**:
    * **Stack-Based Propagation**: Full-grid scans during propagation are highly inefficient $O(N^2)$. To eliminate this bottleneck, this architecture uses a dedicated stack-based propagation array. When a cell collapses or experiences a reduction in its valid state, its immediate coordinate indices are pushed onto a stack. The propagation loop continuously pops cells from this stack, updates their neighbors, and pushes those neighbors onto the stack if their states change. Propagation halts naturally when the stack empties, guaranteeing an optimal $O(K)$ localized traversal where $K$ is the number of affected cells.
    * **Pre-calculated Lookups**: Adjacency logic is compiled into flat, highly cached multi-dimensional array lookups during the engine startup sequence, removing dynamic evaluation overhead during execution.

### How to Run
1. Open the project folder inside the **Unity Editor**.
2. Open the main showcase scene located under `Assets/Scenes/3D_WFC.unity` or under `Assets/Scenes/2D_WFC.unity`.
3. **Enable** the generator you want to try. 
4. Press the **Play** button in the Unity Editor toolbar.
5. **Wait** until the generation has been finished.

---

## 4. Research Findings & Challenges

### Contradiction Handling
A defining architectural pillar of this project is its approach to handling contradictions. Many advanced implementations of WFC employ backtracking architectures, saving snapshots of the grid state at previous decision nodes so the algorithm can rewind, ban the problematic tile choice, and try an alternative path when a dead end occurs.

For this research project, **no backtracking was implemented**. 

If any propagation wave reduces a cell's remaining available tile states to absolute zero, a contradiction is instantly declared. The system immediately halts the propagation stack, discards the half-generated grid, clears all cell states back to their uniform maximum entropy defaults, and completely restarts the entire generation loop from scratch. 

While this design entirely avoids the complex memory allocation patterns and state-tracking overhead required by deep backtracking snapshots, it creates a steep performance dependency on constraint strictness. If your ruleset is highly restrictive, the algorithm may fall into a heavy loop of frequent hard resets before discovering a clean path to full resolution.

### Performance & Benchmark Graph Analysis
To thoroughly understand how constraint styles and grid sizes impact runtime, rigorous benchmark data was recorded across three distinct grid volumes: **5,120 cells**, **25,000 cells**, and **50,000 cells**. The findings are analyzed through the recorded data visualizations below.

#### 1. Categorical Execution Time Analysis (Bar Graphs)
The system was benchmarked under three distinct constraint configurations to measure execution overhead:

* **Graph 1: Base vs. All Constraints** The "Base" configuration represents a skeletal ruleset with minimal connectivity conditions. When switching to "All Constraints" (where complex multi-axis matching rules are enabled), execution times scale up drastically. The propagation wave runs much deeper per cycle because a single collapse strips away a larger percentage of valid states from neighboring cells, forcing the stack to process extensive cascading changes across the map.

<p align="center">
  <img src="https://github.com/user-attachments/assets/d07e4728-c1b4-4aeb-9109-ac21ceecec5f" alt="WFC Chart" width="70%" />
</p>

* **Graph 2: Base vs. Weighted vs. All Constraints** Introducing "Weighted" selection means adding Shannon Entropy calculations and frequency-biased picking to the observation phase. While adding weights increases the mathematical operations required per observation step, the benchmarks show it often results in a cleaner, more predictable generation path than raw random selection. It sits comfortably between the light Base configuration and the highly intense All Constraints model.

<p align="center">
  <img src="https://github.com/user-attachments/assets/e08ce7dd-e083-4a67-8bfd-a959d65c5246" alt="WFC Chart" width="70%" />
</p>

* **Graph 3: Base vs. Negative Constraints vs. All Constraints** "Negative Constraints" explicitly define what tiles *cannot* sit next to each other rather than what *must* connect. Processing negative constraints requires checking against an exclusion matrix. The bar graphs indicate that negative constraint checking introduces a unique footprint: it skips the complex matching overhead of positive pairs but can cause longer propagation runtimes because exclusions create wide, unpredictable ripples across the grid.

<p align="center">
  <img src="https://github.com/user-attachments/assets/02559dbe-7f35-4ee0-98dc-41d193ad818e" alt="WFC Chart" width="70%" />
</p>

#### 2. Scale-Based Runtime Acceleration (Line Graphs)
To see how these configuration settings scale with environment volume, percentage-based runtime tracking was evaluated across three distinct line charts representing **5,120 cells**, **25,000 cells**, and **50,000 cells**.

* **The Scaling Curve**: As the absolute number of grid cells scales up, the execution time scales *superlinearly*. This is caused by the compounding nature of the propagation stack.

<img src="https://github.com/user-attachments/assets/27af7601-20d5-49f2-b462-517deeab710f" width="32%" /> <img src="https://github.com/user-attachments/assets/0d7beaeb-67c8-407e-9ecf-68a9c010ebef" width="32%" /> <img src="https://github.com/user-attachments/assets/365b2784-3a01-45b8-ac7a-a1ef004346ff" width="32%" />

* **The Negative Constraint Bottleneck**: The line charts reveal that the most severe performance penalty does not come from scaling up to "All" constraints, but explicitly from the introduction of **Negative Constraints**. At a modest 5,120 cells, adding negative constraints causes a manageable 154.89% increase in generation time compared to the base baseline. However, as the grid scales up, this tracking logic encounters a massive scaling wall.
* **Algorithmic Cost of Exclusions**: This dramatic spike happens because of how the propagation pipeline handles exclusions on massive fields. While positive rules quickly narrow down choices, negative constraints force the system to iterate through every single neighbor cell and systematically evaluate what *cannot* exist. As the web of connections grows wider over 50,000 cells, processing these exclusion checks across the localized stack turns into a massive computational bottleneck, making the algorithm incredibly heavy before it even reaches the final "All Constraints" tier.

#### 3. Spatial Dimensionality Effects: High vs. Low Y-Level
A specialized benchmark was designed to isolate the impact of spatial layout on performance. The total cell volume was kept exactly identical at **5,120 cells**, but the aspect ratio of the generation box was dramatically altered:
* **High Y-Level Configuration**: A tall, narrow grid layout ($32 \times 5 \times 32 = 5,120$ cells).
* **Low Y-Level Configuration**: A flat, shallow grid layout ($64 \times 2 \times 40 = 5,120$ cells).

The resulting line graph charts the percentage runtime variance between these two identical-volume setups.

<p align="center">
  <img src="https://github.com/user-attachments/assets/03c2cbd3-6009-4b70-abed-cf8a97c96fc8" alt="WFC Chart" width="70%" />
</p>

* **Finding**: The data demonstrates that the **High Y-level configuration takes significantly longer to execute** than the Low Y-level layout.
* **Theoretical Reasoning**: This performance difference is a direct result of grid topology and neighbor surface area. In a flat, low-Y layout, the generation wave quickly hits the top and bottom boundary walls of the grid, truncating the propagation paths along the vertical axis. In contrast, a high-Y layout exposes more vertical layers to active processing. Cells in the middle layers have a higher number of active, unbound neighbors across all axes. As a result, propagation waves can travel out in more dimensional directions simultaneously, creating a longer, more complex chain of updates for the stack to resolve before settling into a stable state.

---

## 5. Visuals & Demos
* **No Constaints**
<p align="center">
  <img src="https://github.com/user-attachments/assets/9c978411-86a7-4ea5-8038-6bad12e0f53a" alt="WFC Chart" width="90%" />
</p>
<br>

* **Weight Constraints**
<p align="center">
  <img src="https://github.com/user-attachments/assets/fb00d484-22cc-41b0-8767-3eb4932e29ba" alt="WFC Chart" width="90%" />
</p>
<br>

* **Negative Constraints**
<p align="center">
  <img src="https://github.com/user-attachments/assets/0b580b61-75a3-4724-8727-1f847f25d2af" alt="WFC Chart" width="90%" />
</p>
<br>

* **Weight and Negative Constraints**
<p align="center">
  <img src="https://github.com/user-attachments/assets/f5c5534f-72b8-45fa-8de4-1d6c681dd25f" alt="WFC Chart" width="90%" />
</p>

* **Map Generation Demo**
<p align="center">
  <img src="https://github.com/user-attachments/assets/0933f9e1-0213-4d1c-a778-f96de3c8aab8" alt="WFC Demo" width="90%" />
</p>


---

## 6. Conclusion
This research project successfully highlights the core mechanics and performance traits of the Wave Function Collapse algorithm within Unity. By optimizing the architecture with a stack-based propagation model, the system handles localized constraints cleanly and generates cohesive layouts.

The performance data collected reveals that WFC runtimes are dictated by much more than just the raw number of cells. Constraint configuration styles and spatial dimensions play an enormous role in how generation waves travel through the system. Opting for a clean slate approach on contradiction (hard resetting) keeps the architecture straightforward and lightweight, but it introduces a distinct scaling bottleneck when dealing with massive grids or strict rulesets. To scale this implementation up for massive worlds, future work should explore localized chunk-based generation or multi-threaded propagation pipelines to keep execution times fast and predictable.

---

## 7. Sources & References
* **Original WFC Repository**: [Maxim Gumin's WFC](https://github.com/mxgmn/WaveFunctionCollapse)
* **Technical Explanation**: [Robert Heaton - The Wavefunction Collapse Algorithm explained](https://robertheaton.com/2018/12/17/wavefunction-collapse-algorithm/)
* **Paper about implementation**: [Tristan Wauthier WFC paper](https://tristanwauthier.com/PDF/GW_2223_Tristan_Wauthier_EN_Paper.pdf)
* **Entropy**: [Shanon Entropy](https://en.wikipedia.org/wiki/Entropy_(information_theory))
