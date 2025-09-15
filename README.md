# Game Design Document (GDD)

Read the [project specification](https://github.com/feit-comp30019/project-2-specification) for details on what _actually_ needs to be covered here. While you should (obviously) delete everything here and replace it with an actual GDD before submitting your project, you may wish to keep a copy of it as a cheat sheet for how to use "GitHub flavoured markdown".

Ensure that _your game design document_ is `well written` and formatted **appropriately**.
Below are examples of markdown features available on GitHub that might be useful, though this is not an exhaustive list, nor is it required that you use all of these features.

### Table of contents

- [Game Overview](#game-overview)
- [Story and Narrative](#story-and-narrative)
- [Gameplay and Mechanics](#gameplay-and-mechanics)
- [Levels and World Design](#levels-and-world-design)
- [Physics](#physics)
- [Art and Audio](#art-and-audio)
- [UI/UX Design](#uiux-design)
- [Technology and Tools](#technology-and-tools)
- [Team Communication](#team-communication)
- [Possible Challenges](#possible-challenges)


### Game Overview
**•	Core Concept**: Our game is a color-based puzzle-platformer where the player navigates a miniature, dreamlike world by collecting and switching between colors. The unique mechanic is that player can only interact with objects that share their current active color. The platforms and switches become solid or intangible depending on the chosen color. This mechanic transforms simple movement into a constantly evolving puzzle, where success depends on mastering color switch,timing and spatial reasoning. The player's role is to journey through this environment, gradually restoring colorfulness and life to the world, with progression symbolized through colors collected along the way. 

**•	Related genre**: The game is a 3D puzzle-platformer genre, with influences from narrative-driven exploration games. Similar games includes It Takes Two (for stylized world and cooperative puzzle feeling) and Monument Valley (for spatial puzzles). Unlike other puzzle-platformers, this game features a unique color-matching mechanic in which colors shape both the world’s physics and the player’s progression. It blends the precision of platforming with a symbolic narrative of emotional healing.

**•	Target Audience**: The target audience is casual players who enjoy puzzle-platformers and atmospheric experiences. The game is especially suited for teenagers and university students who appreciate immersive gameplay and symbolic storytelling. 

**•	Unique Selling Points (USPs)**: 

• Color matching system - Platforms and switches exist only when matched with the player’s active color, creating fresh spatial puzzles.

• Evolving palette - The world shifts from muted blues to vibrant oranges, visually reflecting emotional growth.

• Story mechanic integration – Collecting colors ties emotional healing directly into gameplay.

• Replayability – Time trials encourage speedrunning and mastery beyond the first playthrough.

### Story and Narrative
**•	Backstory**: The world was vibrant and full of color, but a mysterious event drained it, leaving only muted blues and greys. In this fragile, miniature landscape, the player awakens as a small spirit-like figure carrying only one remaining color. Their journey is both literal and symbolic: to recollect the lost color and restore vibrancy to the world. With each color recovered, the environment gradually transforms, reflecting emotional healing and growth. The conflict is not against a villain but against darkness and despair, as the player overcomes challenges that resist the return of light. By the end of the journey, the world shines again in warm, symbolizing victory and renewal.

**•	Characters**: In our game, there is only one character—the player character, named Luma. Luma is a small, spirit-like figure that represents the player’s emotions. As colors gradually return to the world, Luma’s appearance subtly transforms, such as a brighter outfit or a soft glow. His personality is defined by innocence, determination, and adaptability. The player’s purpose through Luma is to restore vibrancy and color to the world.

### Gameplay and Mechanics
**•	Player Perspective**:
The game adopts a third-person perspective. The camera is positioned behind the player character and follows their movements smoothly, providing both spatial awareness of the environment and precise control for platforming.  

**•	Controls**:
The player uses WASD keys for movement, Space to jump, and the left mouse button to switch colors once a color has been collected. The control scheme is intentionally minimalistic to allow players to focus on puzzle-solving rather than complex input sequences.

**•	Progression and Objectives**:
Players must collect and utilize colors to solve puzzles and advance through the environment. As progression continues, puzzles require the use of multiple colors and rapid switching between them. Falling outside the scene results in the player’s “death,” after which they respawn at the latest checkpoint. Upon reaching the final destination, a timer is displayed to indicate total completion time, encouraging replayability and optimization.

**•	Core Mechanics**:
The central mechanic is color-based interaction. The player can only physically interact with objects in the scene that share the same active color. For example, a blue character can stand on blue platforms, push blue blocks, or activate blue switches. Non-matching objects behave as intangible, creating the illusion of phasing through them. This mechanic is the foundation of both puzzle design and progression.
 
### Levels and World Design
**•	Game World**:
The game world consists of one large 3D environment rather than segmented levels. The design emphasizes exploration and continuity, with checkpoints strategically placed to reduce frustration after failure. Although no map system is provided, navigation signs and environmental cues guide players toward their objectives.

**•	Objects and Assets**:
The world features two main categories of objects:
1.	Decorative Objects: Large-scale props, such as oversized cans or static scenery, reinforce the miniature-world theme. These objects cannot be moved and do not interact with the player beyond serving as environmental boundaries or visual storytelling elements.
2.	Puzzle Objects: Interactive objects that respond to the player’s current color. These include platforms, blocks, switches, and moving elements, each requiring correct color alignment to function as intended.

**•	Puzzle and Challenge Design**:
Early puzzles introduce simple color-matching mechanics, while later challenges demand mastery of quick color switching, timed jumps, and multi-step problem solving. Some puzzles incorporate moving platforms or timed events, requiring both precision and strategy. Difficulty ramps up gradually, ensuring players are taught mechanics progressively before being tested on more complex combinations.
 
### Physics
•	Gravity and Jumping:
The player character is constantly influenced by gravity, resulting in natural falling and jumping arcs. Jumps apply an upward impulse that diminishes over time, creating a parabolic trajectory. Falling off platforms or leaving the scene boundary triggers a death-and-respawn cycle at the latest checkpoint.

•	Collision Detection:
All interactive objects have colliders. Collision is conditional on the player’s current color state: objects of matching color behave as solid, while non-matching objects allow the player to pass through. Decorative objects retain static colliders but serve only as barriers, not puzzle elements.

•	Movement and Forces:
Player movement is velocity-based, prioritizing responsive, arcade-style control rather than realistic acceleration. Puzzle blocks and pushable objects move in response to applied forces but remain restricted to simple linear motion for consistency and predictability.

•	Environmental Hazards and Boundaries:
Falling into void areas or outside the playable world results in an immediate reset. Dynamic objects such as moving platforms follow predetermined motion paths, ensuring puzzle timing remains consistent and learnable rather than random.

•	Interaction Logic:
All physics-based interactions are mediated through color. The player’s ability to stand, push, or activate an object depends entirely on matching color states. This creates a unique and intuitive rule system, making color-switching both a mechanical challenge and a core part of the narrative identity.

### Art and Audio
**•	Art Style**:
The visual style of our game takes inspiration from It Takes Two - stylised, slightly surreal, and handcrafted , giving the world a playful yet emotionally impactful atmosphere. The overall color palette of our scene changes as the game progresses. It begins with cold, muted blues and greys symbolising sadness. As the player collects new colors, warmer tones gradually infuse the scene, visually representing healing and growth. The final scene will be orange in color representing triumph and renewal. 

Objects along the path in the scene - such as pencils, books, soda cans - maintain exaggerated, oversized forms to emphasize the miniature theme. Their textures evolve alongisde the player's progress in the game - dull and worn in the beginningm, then gradually infused with brightness, saturation and playful colors as the world evolves into a warm tone. 

Visual References from *It Takes Two*

| ![Visual 1](images/it-takes-two1.png) | ![Visual 2](images/it-takes-two2.png) |
|----------------------------------------|----------------------------------------|
| *Visual 1*                          | *Visual 2*                          |

**•	Sound and Music**:
The game's sound design is deliberately tied to the player's emotional progression and the color-based puzzle mechanic. In early stages, the maze feels hollow and fragile filled with soft wind, and sparse piano notes to reinforce the sense of emptiness and sadness. As new colors are collected, the environment grows richer, so does the music. Gentle chimes, faint hums, and brighter harmonies layer into the soundscape. By the final stage, the world is filled with uplifting and lively ambiences - sparkling tones and birdsong, symbolising renewal and hope.

**•	Assets**:
We will use a combination of custom-created, AI-generated, and sourced assets. 

3D Models - Rigged and animated player character through Mixamo and surrounding objects and collectibles sourced through Unity asset store
Sound - Freesound (https://freesound.org) for sound effects and ambiences, OpenGameArt (https://opengameart.org)

### UI/UX Design
1. 📖 **Information Architecture**
- Main Menu
  - Game Start
  - Chapter Select
  - Settings (Volume / Language)
  - About (Credits)
- Game Flow
  - Player enters → Select Chapter → In-game → Complete Level → Return to Chapter Menu
- Navigation Logic
  - In-game → Pause Menu → Return to Chapter / Continue
	
2. 🎯 **HUD (Minimalist Style)**
- Minimalist style (almost no HUD, only necessary hints).
- Tap feedback (light spark)  
- Path guidance arrows  
- Level complete prompt (minimal text / icon)

3. 🖥 **Menus & Screens**
- Main Menu
  - Minimal logo + "Start" button (centered)
  - Soft background music
- Chapter Select
  - Card-style thumbnails (geometric architecture illustrations)
  - Locked vs. unlocked distinction (bright vs. grayscale)
- Settings
  - Volume slider
  - Language switch
- Pause Menu
  - Continue
  - Return to Chapter

4. 🎮 **Interaction Design**
- Controls
  - Tap: Move character
  - Swipe/Rotate: Manipulate structures
- Feedback
  - Valid tap area → light spark feedback
  - Mechanisms → animation + sound
- Transitions
  - Scene changes → smooth fade-in/out
  - Menus → scale/fade transitions

5. 💡**User Experience**
- **Onboarding:** first level serves as natural tutorial  

6. 🎨**UI Visual Style Guide**
- Color Palette: soft pastel tones  
- Typography: minimalist sans-serif 

### Technology and Tools
- Unity 6.1x - core engine for development of game
- Github - version control, merge codes, code reviews
- Slack - daily team communications and task assignments
- Mixamo - quick rigging and character animations 
- Tripo AI - generate base 3D models from concepts 
- Blender - optimise mesges , refine textures, and polish models 

### Team Communication
We will use Slack as our main communication platform for daily updates, discussion, organising and assigning tasks. All communication will be in English to ensure inclusivity and transparency for both the team and teaching staff.

Timeline:
- Week 6-7: Discuss and finalise game design document
- Week 8-10: Assets integration and imported to unity, core gameplay loop developed (jumping, movements, challenges)
- Week 11: Work on gameplay video and upload to youtube 
- Week 12: Final polishes to the game 

### Possible Challenges
- WebGL Performance: Models generated through Tripo may be too heavy causing longer loading times
    Plan: optimise assets using Blender (compress textures)
- Time Constraints: limited weeks to complete the project
    Plan: keep scope tight - focus on one single long scene and polish it instead of having multiple unpolished scene

