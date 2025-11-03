# Project 2 Report

Read the [project 2
specification](https://github.com/feit-comp30019/project-2-specification) for
details on what needs to be covered here. You may modify this template as you
see fit, but please keep the same general structure and headings.

Remember that you should maintain the Game Design Document (GDD) in the
`README.md` file (as discussed in the specification). We've provided a
placeholder for it [here](README.md).

## Table of Contents

- [Evaluation Plan](#evaluation-plan)
- [Evaluation Report](#evaluation-report)
- [Shaders and Special Effects](#shaders-and-special-effects)
- [Summary of Contributions](#summary-of-contributions)
- [References and External Resources](#references-and-external-resources)

## Evaluation Plan
**•	Evaluation techniques**:

Observational technique - Think aloud

We will ask 5 participants to play the game while verbalising their thoughts aloud. This technique helps us understand players’ reasoning and identify usability problems, such as confusing controls or unclear objectives, by directly observing their behaviour and listening to their thought process. During the session, we will record the screen and take notes on where players hesitate or get stuck.

Query technique - Post game questionnaires

After finishing the play session, 5 different participants will complete the GUESS-18 (Game User Experience Satisfaction Scale) questionnaire. This technique is useful for gathering quantitative feedback on enjoyment, difficulty, and visual appeal, as well as what players liked or disliked. It allows us to evaluate players’ overall satisfaction and perceived fun of the game.

**•	Participants**:

We will recruit 10 participants (5 for Think-Aloud, 5 for GUESS-18), mainly from the University of Melbourne student community, aged 18–30. This group represents our target audience, as the game is aimed at young adults who enjoy puzzle-solving based games. Participants will be recruited through class announcements and online student groups. They should have basic gaming experience but no prior knowledge of our game to ensure unbiased feedback.

**•	Data collection**:

Both qualitative and quantitative data will be collected to evaluate player experience. Qualitative data will include players’ spoken thoughts, comments, and observed behaviours during the Think-Aloud sessions, helping to identify usability issues and emotional reactions. Quantitative data will come from participants’ responses to the GUESS-18 questionnaire, which provides measurable scores on enjoyment, playability and usability.

Gameplay sessions will be screen-recorded while participants verbalise their thoughts, and observer notes will be taken on key events or difficulties.

After gameplay, participants will complete a Google Form version of the GUESS-18 survey, allowing responses to be stored digitally and exported for analysis. Data will be collected and organised using built-in screen recording, Google Forms, and Excel, with additional qualitative insights documented through observation notes.

**•	Data analysis**:

We will analyse both qualitative and quantitative data to evaluate the game’s usability and overall player experience.

Qualitative data from the Think-Aloud sessions will be reviewed through thematic analysis. We will watch the recorded sessions and patterns such as confusion points, navigation issues, or emotional reactions. These insights will help identify specific design problems, such as unclear controls or visual feedback, and guide improvements. 

Quantitative data from the GUESS-18 questionnaire will be analysed using descriptive statistics (mean and standard deviation) for each dimension—enjoyment, playability, usability, aesthetics, and immersion.

Average scores below a set threshold (e.g., 3.5/5) will indicate areas needing refinement. We will also compare participants’ written comments to interpret the reasons behind lower ratings.

**•	Timeline**:

The evaluation process will take place over two weeks. During Week 10, we will recruit participants and conduct the Think-Aloud sessions, collecting qualitative data on usability and player experience. In Week 11, we will distribute the GUESS-18 questionnaire to a separate group of participants and collect quantitative feedback. Once all data has been gathered, we will perform data analysisin the following days, identifying key usability and design issues. Game refinements based on these findings will be implemented before the final submission deadline, ensuring that improvements are informed by real user feedback.

**•	Responsibilities**:

One member will be responsible for organising and conducting the Think-Aloud sessions, including participant coordination, screen recording, and taking observation notes. Another member will handle the GUESS-18 questionnaire, creating the Google Form, distributing it to participants, and collecting responses.

All team members will collaboratively analyse the data. Finally, the whole team will participate in discussing and implementing game improvements based on the evaluation findings.

To ensure equal contribution, we will:

• Assign clear, documented responsibilities for each task.

• Conduct regular to monitor progress and share updates.

• Make all evaluation materials (recordings, forms, notes) accessible to all team members for transparency.

## Evaluation Report

The goal of our evaluation was to identifu usability problems, gameplay clarify issue and design weaknesses taht affected player enjoyment and immersion. We used both observational and survey-based methods to gather detailed feedback, and we implemented serval targeted improvements based on the results. 

**•	Findings and improvemnts**:

• Objective and gameplay clarity: Many players were unsure about their goals when a level started and could not easily distinguish interactable objects.

Improvements: Added instructional panels explaining level objectives. Glowing phones were made interactive, and proximity prompts now trigger new gameplay mechanics to help players understand progression.

• Atmosphere and immersion: Players noted the absence of a background story and music, which made the experience feel flat and less engaging.

Improvements: Introduced a short background story at game start, added background music and movement sound effects, and implemented additional shaders to make visuals more appealing and immersive.

• Game design and navigation: Some players reported getting lost or needing to walk long distances to return after falling off the table. Others found early puzzles too simple.

Improvements:
Added guiding particle effects and shortcuts to help players return to the correct path quickly. Designed three new levels with progressive difficulty and introduced new puzzle elements, such as movable books that open paths, traps that break after jumps, and more interactions between World A and World B objects.

• Feedback and reward system: Players were uncertain when they had successfully completed an objective, as there was no clear success indicator.

Improvements: Added visual “Victory” message and audio feedback for successful interactions at the end of the game to clearly signal completion.

• Controls and responsiveness: Participants found the controls not straightforward. And the absence of smooth mouse control for adjusting the camera limited participants' navigation flexibility.

Improvements: Implemented mouse-based camera control, allowing players to freely and smoothly adjust their view during gameplay. Added on-screen UI prompts such as “Press E to view the instruction”, guiding players through core controls and improving overall responsiveness.

• Bug and collision issues: Some players were able to pass through certain obstacles or triggers failed to activate.

Improvements: Refined collider boundaries, adjusted trigger sizes, and tested all interactable objects to ensure consistent activation.

## Shaders and Special Effects

TODO - see specification for details

## Summary of Contributions

TODO - see specification for details

**Rebecca**: Primarily responsible for character implementation, user interface design, and sound integration.
- Character Implementation: Imported and configured the main player model from Mixamo, including all associated animations (walk, run, jump). Developed and refined the player movement script to ensure smooth transitions between animations and responsive control.
- Camera Follow & Additional Animations: Helped with adjusting third-person camera follow system to achieve smoother movement and mouse control. Additionally implemented the player's dance animation switcher - allowing player to trigger and switch between multiple animations such as HipHop dance and Sad idle - to visually express the player's emotions throughout the game, enhancing the storytelling and presentation in the gameplay video.
- User Interface Design: Designed and implemented - Main Menu, Settings Menu, Pause Menu, Boss Health Bar, Victory Screen, and Introduction Story panel. Each interface was integrated with button logic for navigation and settings control. 
- Background Music & Sound Effects: Imported and integrated multiple background music tracks (sad, happy, victory) and sound effects such as footsteps for the player, button clicks, and keyboard typing sounds for the story panel. Implemented the logic for background music switching bewteen both worlds (A and B) and synchronized UI sound feedback.
- Puzzle Logic: Developed one puzzle interactions - when player jumps on a stack of books, the stack changes color on the first jump and explodes on the second. Also incorporated the particle effect for the explosion, the particle system effect for the explosion was implementation by another teammate. 

## References and External Resources

TODO - see specification for details
