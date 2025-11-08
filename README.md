# Comparative AI in Fighting Games — Machine Learning vs Rule-Based Systems

![Street Fighter Banner](https://github.com/SuhaibAnwaar/MLAgent-vs-RBS-StreetFighter/blob/3842ecd43f6d3f7ebed5175cc33f0cfebdadca2b/StreetFighter.png)

Video demonstration: https://youtu.be/3l_88zcaQds

Author: Suhaib Anwaar

Project summary
- Comparative implementation and user study of two opponent AI approaches for a 1v1 fighting game in Unity:
  - ML agent: reinforcement learning (Unity ML-Agents, neural network policy) trained to fight and adapt to player patterns.
  - RBS (Rule-Based System): deterministic heuristics for movement and actions (punch, kick, block, jump, retreat).
- Goal: evaluate playability, challenge, and player experience when using ML-driven opponents versus a hand-crafted RBS baseline.

Core gameplay
- Match structure: 5 rounds.
- Combo system: players (and AI) have a combo attack that has a cooldown.
- Win/lose conditions: defeat opponent by depleting their health.
  
AI behavior overview
- ML agent (reinforcement learning):
  - Trained for many hours against the RBS baseline.
  - Learns aggressive behaviors: frequent punches and kicks, and uses combo moves immediately on cooldown.
- RBS (rule-based system):
  - Deterministic rules: approach player, attempt attack (punch/kick), block, jump or retreat depending on simple conditions.

Key findings & player feedback
- Behavioral analytics and user testing indicate that ML agents provide a more engaging and challenging experience compared to the RBS baseline.
- Players generally preferred matches against the ML agent because it adapted and presented varied behaviors over time.
- Data analysis showed measurable improvements in player engagement and challenge adaptation when ML opponents were used
  
Training, rewards & design notes (ML agent)
- Training environment: Unity scenes with simulated player behaviors (using the RBS and scripted play patterns) and varied spawn/position seeds.
- Reward shaping:
  - Positive reward for dealing damage and reducing opponent health.
  - Positive reward for winning a round.
  - Small positive reward for successful combo hits.
  - Penalty for taking damage, and larger penalty for losing a round.
- Policy objective prioritized winning and reliability; this resulted in aggressive, high-frequency attack policies and instant combo use on cooldown.

Limitations observed
- ML agent tends to spam punch/kick and uses combos immediately on cooldown, regardless of precise optimal spacing; this makes it harder but less "human-like".
- ML agent sometimes attempts attacks when out of ideal range; future training should emphasize range awareness.
- RBS is  simple; it can be improved with predictive heuristics or planning to provide a stronger engineered baseline.

Future work
- Constrain combo/attack usage with range and state checks so ML agent uses close-range moves only when appropriate (reduce spam).
- Reward shaping additions: penalize unnecessary movement or long, inefficient attack attempts; reward precision and efficient positioning.
- Expand RBS complexity: add timed combos, predictive blocking, and conditional retreat strategies to create a stronger baseline.
- Add more combos and varied move-sets to thr game

Technical
- Engine: Unity
- ML training: Unity ML-Agents
- Languages: C#

