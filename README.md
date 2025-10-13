# Game Design Document (GDD)

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
**•	Core Concept**: The core concept of the game is escaping a miniature dual-world that represents the mental state of a student rushing to finish an assignment. The player takes on the role of a University of Melbourne student who has been mysteriously shrunken and trapped on his own study desk as the deadline approaches. To return to normal size and restore color to his world, he must navigate between two contrasting miniature realities — the grey, rainy World A, symbolizing stress and pressure, and the colorful World B, representing relief and clarity after completing the task. By switching between these two worlds, solving puzzles, and overcoming platforming challenges, the player symbolically experiences the journey of overcoming academic stress and finding balance again.

**•	Related genre**: The game is a 3D puzzle-platformer genre, with influences from narrative-driven exploration games. Similar games includes It Takes Two (for stylized world and cooperative puzzle feeling) and Monument Valley (for spatial puzzles). Unlike other puzzle-platformers, this game has the combination of world switching, levitation based interaction, and the symbolic contrast between the grey “due world” and the colorful “after-due world” gives the game a distinct emotional and visual identity that ties everyday student struggles to a creative and immersive gameplay experience.

**•	Target Audience**: The target audience is casual players who enjoy puzzle-platformers and atmospheric experiences. The game is especially suited for teenagers and university students who appreciate immersive gameplay and symbolic storytelling. 

**•	Unique Selling Points (USPs)**: 

• World-switching system – Objects, platforms, and bridges appear or disappear depending on the active world (A or B), creating dynamic and perspective based spatial puzzles.

• Emotional storytelling integration – The transition from the grey, rainy world to the colorful world symbolizes the player’s emotional journey from stress to relief as the assignment nears completion.

• Replayability – Time trials encourage speedrunning and mastery beyond the first playthrough.

### Story and Narrative
**•	Backstory**: The story follows a University of Melbourne student who has been mysteriously shrunken and trapped within a miniature version of his study desk. As the deadline for his assignment approaches, his mind fractures the world into two contrasting realities — World A, a grey, stormy space that mirrors his stress and exhaustion, and World B, a colorful, peaceful world representing focus and relief after finishing his work. The entire game takes place on a miniature study desk world, where everyday objects like books, stationery, coffee mugs, and papers become giant structures, platforms, and obstacles. Rain, reflections, and lighting effects help distinguish the emotional tone of each world.

**•	Characters**: In our game, there is only one character—the player character, named Luma. Luma is a University of Melbourne student, a hardworking but overwhelmed individual trapped in a miniature version of his study desk. He wears simple everyday clothes — a hoodie, jeans, and sneakers — and his appearance reflects his emotional state: dull and desaturated in World A, colorful and lively in World B. His motivation is to finish his assignment and escape the stormy, stressful world that represents his anxiety. The student’s journey symbolizes determination, perseverance, and emotional growth through self-discipline and problem-solving.

### Gameplay and Mechanics
**•	Player Perspective**:
The game adopts a third-person perspective. The camera is positioned behind the player character and follows their movements smoothly, providing both spatial awareness of the environment and precise control for platforming.  

**•	Controls**:
The player uses WASD to move, Space to jump, Q switch between world A and world B, the left mouse button to control the camera, and the right mouse button to levitate levitatable objects. The control scheme is intentionally minimalistic to allow players to focus on puzzle-solving rather than complex input sequences.

**•	Progression and Objectives**:
Players must solve puzzles and advance through the environment. As progression continues, puzzles require the use of multiple colors and rapid switching between them. Falling outside the scene results in the player’s “death,” after which they respawn at the latest checkpoint. Upon reaching the final destination, a timer is displayed to indicate total completion time, encouraging replayability and optimization.

**•	Core Mechanics**:
The game is a puzzle–platformer where the player switches between two parallel worlds to overcome obstacles and reach the goal. In the grey, rainy World A, colors have faded, while in the vibrant World B, the environment is restored and new structures like bridges appear. The player can move, jump, switch worlds, and use telekinetic levitation to lift and reposition objects to solve puzzles or create new paths. The core rule of the game is that some objects only exist in one world at a time, and switching worlds changes the environment’s layout without altering physics or player position. The main fun comes from combining world-switching and object manipulation to discover hidden routes and solve creative challenges. 
 
### Levels and World Design
**•	Game World**:
The game world consists of one large 3D environment rather than segmented levels. The design emphasizes exploration and continuity, with checkpoints strategically placed to reduce frustration after failure. Although no map system is provided, navigation signs and environmental cues guide players toward their objectives.

**•	Objects and Assets**:
The world features two main categories of objects:
1.	Decorative Objects: Large-scale props, such as oversized cans or static scenery, reinforce the miniature-world theme. These objects cannot be moved and do not interact with the player beyond serving as environmental boundaries or visual storytelling elements.
2.	Puzzle Objects: Interactive objects that respond to the player’s current world state. These include platforms, blocks, switches, and moving elements.

**•	Puzzle and Challenge Design**:
Early puzzles introduce simple world-switching mechanics, teaching players how objects appear or disappear between the two worlds. As the game progresses, challenges become more complex, combining timed jumps, object levitation, and multi-step interactions that require switching worlds at the right moment. Some puzzles involve moving platforms or timed environmental changes, demanding both precision and planning. The difficulty increases gradually, ensuring players learn each mechanic before being tested on more advanced combinations of movement, timing, and spatial reasoning.
 
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

