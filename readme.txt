This Kerbal Space Program Plugin is in open beta testing.  The various constant variables are stored in the KerbalFeels\Plugins\PluginData\KerbalFeels\KF_Constants.cfg file.  If you feel the default values need tweaking feel free to experiment and let me know the results at forum.kerbalspaceprogram.com, user wpatter6.
	
----
To Install:
Download and unzip the KerbalFeels 0.1.0.zip file and copy the KerbalFeels folder into the GameData base folder, and start up KSP.

----
Copyright (c) 2015, wpatter6

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
----
Description:

Based on the different personalities of the kerbals (and RNG), kerbals will begin to like or dislike each other after spending time with each other in spacecraft. Kerbals who have very different stupidity levels may be more likely to have negative results, while a non-courageous kerbal may take a quick liking to one who is more courageous (though that feeling may not be reciprocated!)

Once they reach a certain threshold of liking (or disliking) another kerbal, they will gain a "feeling" towards the other kerbal. There are three different positive feelings and three negative feelings which are chosen at random once the threshold is passed. When they are on a mission, they will gain or lose personality traits and experience levels based on their feelings towards the other kerbals on their mission. Feelings change relatively slowly, so it may take more than a few days on a mission together before the kerbals reach the feeling threshold.These feelings are:

Positive traits give +1 experience level:

Playful: Raises stupidity
In Love: Raises stupidity and courage
Inspired: Lowers stupidity


Negative traits give -1 experience level:

Scared: Lowers courage
Annoyed: Raises stupidity
Hateful: Raises stupidity and courage


These changes aren't permanent, and are only in effect while the kerbal(s) they have the feelings towards are sharing a vessel with them.

Kerbals also have a certain amount of "sanity" which will depreciate over longer missions. It will go down faster if they have negative feelings towards kerbals in their vessel, and slower if there are more kerbals they feel positively towards in the ship. Lower sanity will increase the amount that RNG impacts their feelings towards the other kerbals at the end of the mission. My goal is for sanity to play a few different roles in the future as well, mentioned below.

If a Kerbal's sanity drops too low, there is a small chance they will commit suicide, or if they're in a vessel with another kerbal whom they have negative feelings towards, there is a chance they will murder the kerbal they dislike.

If a murder occurs, the rest of the crew's feelings towards the attacker will be tallied and they will either let the killer go free, "arrest" them (they will appear as missing), or execute the attacker on the spot, depending on how the group as a whole feels about the attacker.

Death of a kerbal will result in a loss of courage for kerbals who had positive feelings toward them.  If their courage drops to 0 and they witnessed the death they will lose an experience level.  These effects last for a random amount of time between 7 days and 6 months depending on what feeling they had towards the kerbal who died.

The GUI is accessable through the Toolbar icon (shaped like a heart) and will show the kerbals and their statistics, as well as their feelings towards other kerbals.

Todo List:
-Parts ideas:
-- Recreational/Athletic/general activity parts for ships and space stations will reduce negative feelings and improve (or possibly even reduce) the crew's sanity
-- Brain scan gives science and is either affected by or has effects on kerbal's sanity -- I think it would be neat if you could get more science if your kerbal is less sane, but not sure how feasable that is
-Feelings impact the amount of experience gained from a mission (inspired and hateful give more xp; playful and annoyed give less)
-Tweaking to constant variables after feedback