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
**•	Core Concept**: The core concept of the game is escaping a miniature dual-world that represents the mental state of a student rushing to finish an assignment. The player takes on the role of a University of Melbourne student who has been mysteriously shrunken and trapped on his own study desk as the deadline approaches. To return to normal size and restore color to his world, he must navigate between two contrasting miniature realities — the dim, late-night World A, illuminated by cool blue light and cluttered with unfinished work, symbolizing stress and pressure, and the colorful World B, representing relief and clarity after the deadline. By switching between these two worlds, solving puzzles, and overcoming platforming challenges, the player symbolically experiences the journey of overcoming academic stress and finding balance again.

**•	Related genre**: The game is a 3D puzzle-platformer genre, with influences from narrative-driven exploration games. Similar games includes It Takes Two (for stylized world and cooperative puzzle feeling) and Monument Valley (for spatial puzzles). Unlike other puzzle-platformers, this game has the combination of world switching, levitation based interaction, and the symbolic contrast between the grey “due world” and the colorful “after-due world” gives the game a distinct emotional and visual identity that ties everyday student struggles to a creative and immersive gameplay experience.

**•	Target Audience**: The target audience is casual players who enjoy puzzle-platformers and atmospheric experiences. The game is especially suited for teenagers and university students who appreciate immersive gameplay and symbolic storytelling. 

**•	Unique Selling Points (USPs)**: 

• World-switching system – Objects, platforms, and bridges appear or disappear depending on the active world (A or B), creating dynamic and perspective based spatial puzzles.

• Emotional storytelling integration – The transition from the grey, dim world to the colorful, bright, sun-lit world symbolizes the player’s emotional journey from stress to relief as the assignment nears completion.

• Replayability – Time trials encourage speedrunning and mastery beyond the first playthrough.

### Story and Narrative
**•	Backstory**: The story follows a University of Melbourne student who has been mysteriously shrunken and trapped within a miniature version of his study desk. As the deadline for his assignment approaches, his mind fractures the world into two contrasting realities — World A, a dim, late-night space that mirrors his stress and exhaustion, and World B, a bright, warm, and peaceful world representing focus and relief after finishing his work. The game unfolds across a miniature apartment environment, expanding beyond the study desk into surrounding areas such as the floor, furniture, and living space. Everyday household objects like books, stationery, coffee mugs, and papers become giant structures, platforms, and obstacles. Reflections and lighting effects help distinguish the emotional tone of each world.

**•	Characters**: In our game, there is only one character—the player character, named Luma. Luma is a University of Melbourne student, a hardworking but overwhelmed individual trapped in a miniature version of his study desk. He wears simple everyday clothes — a hoodie, jeans, and sneakers — and his appearance reflects his emotional state: dull and desaturated in World A, colorful and lively in World B. His motivation is to finish his assignment and escape the dull, stressful world that represents his anxiety. The student’s journey symbolizes determination, perseverance, and emotional growth through self-discipline and problem-solving.

### Gameplay and Mechanics
**•	Player Perspective**:
The game adopts a third-person perspective, with the camera positioned slightly behind and above the player character. This setup provides both spatial awareness and precise control during platforming. The camera follows smoothly and adjusts dynamically during movement, jumps, or interactions to maintain optimal visibility and cinematic flow.

**•	Controls**:
Players use WASD to move, Space to jump, Q to open the portal and switch between World A and World B, the left mouse button to interact with or activate movable objects.
The control scheme remains deliberately minimalistic, encouraging focus on spatial reasoning and puzzle-solving rather than complex input sequences.

**•	Progression and Objectives**:
Players progress through continuous exploration and puzzle-solving rather than by stages or score. The game features no timer and no traditional checkpoints.
When the player falls off the map, they do not die — instead, they are guided back through a shortcut route that naturally leads toward the previous playable area. Subtle guiding lights and ambient visual cues indicate the recovery path, ensuring smooth re-entry into the main route.
The objective is to explore, solve spatial puzzles, and ultimately defeat the boss at the end of the journey. The emphasis is on curiosity, rhythm, and discovery instead of time pressure or point accumulation.

**•	Core Mechanics**:
The central gameplay loop revolves around switching between two overlapping worlds using a portal to overcome environmental challenges.
- World A is grey, broken, and rainy — a world drained of color and life.
- World B is bright and vivid, restoring lost structures, bridges, and platforms.
Unlike conventional dual-world mechanics, objects from World A remain physically solid even while in World B, allowing for layered traversal and creative path-building between dimensions.
The player can use telekinetic manipulation to reposition certain objects into predefined locations or sequences, constructing new routes or triggering hidden mechanisms.
The experience culminates in a boss encounter, where the player must use environmental elements to reflect projectiles back at the boss.
The gameplay rhythm centers on observing, switching, manipulating, and reflecting — a balance of logic and creativity.
 
### Levels and World Design
**•	Game World**:
The game takes place in a continuous 3D environment rather than segmented levels. The world is interconnected and encourages exploration and backtracking. There is no map or checkpoint system; instead, players are guided through visual directionality such as lighting, reflections, particle effects, and color contrast.
When players fall or take a wrong turn, glowing light paths and subtle environmental effects guide them back toward the main route, maintaining immersion and flow without punishment.

**•	Objects and Assets**:
The world features two main categories of objects:
1.	Decorative Objects: Large, static props — such as abandoned architecture, oversized household items, or environmental landmarks — that establish the miniature-world theme. They are non-interactive and serve only as physical boundaries or storytelling elements.
2.	Interactive and Puzzle Objects: Elements that respond to the player’s actions or world state, including levitatable cubes, activation tiles, fragile or single-use platforms, switches, and reflective surfaces. Their behavior changes dynamically between World A and World B.

**•	Puzzle and Challenge Design**:
Early puzzles introduce the world-switching mechanic, teaching players how objects appear, disappear, or overlap between worlds.
As the game progresses, puzzles grow in complexity — requiring players to combine precise timing, levitation, and movement:
- Some challenges involve stepping on activation plates to create temporary paths.
- Others feature fragile or one-time platforms that crumble after use.
- Certain sections require moving or aligning multiple objects to unlock routes.
The final boss battle integrates all learned systems — world-switching, levitation, and projectile reflection — testing the player’s mastery of timing and spatial logic.
The difficulty curve follows a “learn → apply → innovate” pattern, ensuring gradual understanding and rewarding creative experimentation.
 
### Physics
•	Gravity and Jumping:
The player character is always affected by global gravity. Jumps apply an upward impulse that decays smoothly, producing a natural parabolic arc.
Falling into the void does not cause death; instead, the player is gently redirected toward a recovery shortcut, illuminated by guiding lights that visually lead back to the playable area. This system reinforces exploration and reduces frustration from failed jumps.

•	Collision Detection:
All interactive objects have colliders, with their behavior depending on world state:
- In World A, A-world objects are solid while B-world objects are visible but intangible.
- In World B, both B-world and A-world objects remain solid, allowing combined physical interactions and layered traversal possibilities.
Decorative objects maintain static colliders but serve purely as environmental boundaries.

•	Movement and Forces:
Player motion uses velocity-based physics for responsiveness rather than realistic acceleration.
Levitatable or pushable objects move along predefined axes to maintain predictability and consistency during puzzles, ensuring clarity of intent and avoiding chaotic physics behavior.

•	Environmental Hazards and Boundaries:
Falling outside the playable world triggers the guiding-light recovery sequence, visually directing the player back to the main route.
Moving platforms and dynamic elements follow consistent, looped motion paths to ensure puzzles depend on timing and observation, not randomness.
During the boss encounter, projectiles exert physical knockback and can be reflected to damage the boss, reinforcing interactive physics as both threat and tool.

•	Interaction Logic:
All physical interactions are governed by the portal and world-state system.
The player’s ability to stand on, push, levitate, or reflect an object depends entirely on the current world visibility and overlap rules.
This transforms world-switching from a visual effect into a core physical law — the foundation of both the gameplay’s puzzle design and its thematic identity.

### Art and Audio
**•	Art Style**:
Our game has a stylised, low-poly aesthetic set inside a household environment inspired by modern apartment interiors. All props - from desk and beanbags to stacks of books and computers - are built from simplified geometry with clean, angular edges and flat shading. The contrast between the dull blue tones of World A and the warm light of World B visually conveys the emotional shift from late-night stress to morning relief.

The game draws inspiration from It Takes Two - particularly its sense of scale, imaginative household settings and playful tone. However, unlike It Takes Two's detailed, painterly visuals, our world uses low-poly approach that simplifies shapes and textures.

The game begins in the dark, evoking a late-night atmosphere through cool blues, greys, and muted browns. When the player activates the portal sphere, it expands outwards surrounding the player, transforming the world from night to day.

Objects along the path in the scene - such as pencils, books, computers - maintain exaggerated, oversized forms to emphasize the miniature theme. Their colors evolve alongisde the player's progress in the game - dull and desaturated at the beginning, then becoming brighter, more saturated and playful as the portal opens. The color palette of the environment shifts dramatically into warm sunlight, oranges, and yellows, symbolising renewal and hope after completing the dues.

Overall, the aesthetic blends the imaginative scale and narrative charm of It Takes Two with the clarity and warmth of low-poly design, delivering a miniature world that feels handcrafted. playful, and emotionally uplifting.

Visual References from *It Takes Two*

| ![Visual 1](images/it-takes-two1.png) | ![Visual 2](images/it-takes-two2.png) |
|----------------------------------------|----------------------------------------|
| *Visual 1*                          | *Visual 2*                          |

**•	Sound and Music**:
The sound reflects the player's emotional journey, from quiet isolation, to discovery, to truimph. The goal is to have the sound evolve with the visual tone of the surroundings.

Background Music
1. Night Phase - "Sad and emotional"
- plays at the beginning of the game when it is in night mode
- slow tempo, soft piano and ambient synths

2. Portal Phase - "Bright and uplifting"
- begins when portal sphere is activated 
- slightly faster tempo 
- symbolises optimism as the room fills with daylight and world becomes colorful

3. Victory Phase - "Celebration"
- plays when player reaches glowing teddy bear
- energetic rhythm with upbeat melodies
- marks emotional closure and joy after completing all assignments and completing university journey

Sound Effects
- footsteps of the player

UI interaction:
- interactive digital button clicks when player clicks on buttons in the UI 

**•	Assets**:
We combine low-poly 3D models, hand-placed props, and sourced effects to create a cohesive miniature world.

3D Models - Rigged and animated player character through Mixamo and surrounding objects and collectibles sourced through Unity Asset Store
Sound - Freesound (https://freesound.org) for sound effects and ambiences, OpenGameArt (https://opengameart.org), Unity Asset Store, Pixabay (https://pixabay.com)

### UI/UX Design
1. 📖 **Information Architecture**
- Main Menu
  - Play
  - Settings (Volume / Quality)
  - Quit
- Game Flow
  - Player enters → Click "Play" → In-game → Complete Game → Return to Menu
- Navigation Logic
  - In-game → Pause Menu → Resume / Settings / Quit
	
2. 🎯 **HUD (Minimalist Style)**
- Minimalist style
- Only essential hints appear on screen - such as "Press E" prompts when near interactable objects
- Tap feedback (light spark)  
- Path guidance arrows  
- Victory is indicated through an on-screen prompt and return to menu button

3. 🖥 **Menus & Screens**
- Main Menu
  - Minimal game title + "Start", "Settings", "Quit" buttons (centered)
  - darkened game environment image as background
- Settings
  - Volume slider
  - Quality dropdown
- Pause Menu
  - Resume
  - Settings
  - Quit
- Victory Screen
  - Congratulatory message
  - Return to Menu button

4. 🎮 **Interaction Design**
- Controls
  - WASD/Arrow keys: Move character
  - Spacebar - jump
  - E - view instructions when near prompts
  - Esc - Pause Menu
  - Swipe/Rotate: Manipulate structures
- Feedback
  - Interact prompts appear automatically when player is near active objects
  - Valid tap area → light spark feedback
  - Mechanisms → animation + sound
- Transitions
  - Scene changes → smooth fade-in/out
  - Menus → scale/fade transitions

5. 💡**User Experience**
- **Onboarding:** Intro story pop-up explains the story behind this game  

6. 🎨**UI Visual Style Guide**
- Color Palette: soft pastel tones (mainly green, orange and blue)
- Typography: minimalist sans-serif (Luckiest Guy, Titan One)

### Technology and Tools
- Unity 6.1x - core engine for development of game
- Github - version control, merge codes, code reviews
- Slack - daily team communications and task assignments
- Mixamo - quick rigging and character animations 

### Team Communication
We will use Slack as our main communication platform for daily updates, discussion, organising and assigning tasks. All communication will be in English to ensure inclusivity and transparency for both the team and teaching staff.

Timeline:
- Week 6-7: Discuss and finalise game design document
- Week 8-10: Assets integration and imported to unity, core gameplay loop developed (jumping, movements, challenges)
- Week 11: Work on gameplay video and upload to youtube 
- Week 12: Final polishes to the game 
- SWOTVAC: Fix minor erros and bugs in the game so that it's ready to be build and run in WebGL

### Possible Challenges
- WebGL Performance: Models generated through Tripo may be too heavy causing longer loading times
    Plan: optimise assets using Blender (compress textures)
- Time Constraints: limited weeks to complete the project
    Plan: keep scope tight - focus on one single long scene and polish it instead of having multiple unpolished scene

