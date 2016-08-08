#ICFP 2016

Team cashto is, as it has been since 2007, the work of one Chris Ashton from Seattle.

##By the numbers:

* 3528 total problems
* 990 solved perfectly
* 1094 solved > 0.8 resemblance
* 851 solved > 0.5 resemblance
* 496 solved > 0.0 resemblance
* 97 unsolved
* 36 problems submitted (which I am not allowed to solve)

Statistics:

* 29th place when the leaderboard froze
* Solver code: 1281 sloc + 275 sloc of tests
* Automated submission code: 221 sloc.
* Numerous other batch files, scripts, etc.
* ~50 hours of coding

##Overview

The contest problem was to write a program which, given a silhouette of a target origami shape, can determine the folds necessary to re-create that shape from a square piece of paper.  Initially, the problem set consisted of 100 examples given by the organizers; after the first 24 hours of the contest, contestants could begin submitting their own examples for other teams to solve.

As usual, my natural instinct is to use search.  I view a "fold" as dividing a polygon along a line, and then flipping one of the polygons.  Since the initial piece of paper is a convex polygon, and dividing a convex polygon results in two convex polygons, my code for dividing a polygon didn't have to deal with thorny issues around non-convex polygons, nor did the code to determine if a point lay inside or outside a polygon.

Along the way, I had to implement my own RationalNumber and Matrix classes, as they don't exist in C#.  RationalNumber initially used longs, but the switch to BigInteger was relatively painless.

I was unable to implement a working solver during the lightning round, so for the lightning round, I submitted a blank, unfolded piece of paper for every example problem -- the moral equivalent of marking "c" for every question on a multiple-choice exam.

I was able to start churning out my own problems immediately after the end of the lightning round. Submitted problems were essentially a piece of paper folded multiple times at random angles, then rotated by a 3-4-5 pythagorean triangle and given some arbitrary translation. Due to some confusion about timestamp<->UTC mapping, I did not submit any problems for about 10 hours in the middle of the contest.

I didn't have much familiarity with origami prior to the contest, so my code only did one kind of fold, which essentially folds the entire shape across one line. I was vaguely aware that other folds existed, but figured they could be ignored.  During the 2nd day, my goal was to perfectly solve as many of the first 100 example problems as I could -- given that perfect solutions give much more points than imperfect ones.  As I understood the scoring: if 50 other teams solve a problem perfectly, then solving that problem 100% is 50x more valuable than solving it 99.9%.  The problems gradually increase in difficulty, and I could just make gradual improvements and fix bugs and edge cases and then I would be ready to go to town on other team's solutions.
	
Then I got to problem 17.  

FUCK PROBLEM 17.  Problem 17 crushed all of my dreams.  Problem 17 had a squash fold in it, which made a mockery of my whole plan up until that point.  In single moment, all of the search code, all of the tricky code to compare source and target shapes, was reduced to rubbish by problem 17.  As I sat and stared at an example of problem 17 in my hands for TWO FUCKING HOURS, it taunted me even then: "I'm going to waste your time until the end of the contest. You will never, ever, be able to write a program that generates me, even though I'm just three simple folds".

Seriously, fuck this piece of shit:

![Problem 17](https://dl.dropboxusercontent.com/u/31272201/icfp/2016/p17.jpg).

The vision of creating exact solutions went out the window.  I briefly flirted with the idea of building up a square from possible polygons, rather than the current approach of starting with a square and folding it down to a shape -- but at this point it was far too radical a rethink of the strategy.

With a heavy heart, in the early hours of day 3 (which was 2am local time), I moved on to plan B, which was this: it turns out that creating convex origami shapes is really easy -- you can just fold along the edges until you reproduce the shape.  Instead of trying to solve the silhouette, I could just try to solve the convex hull of the silhouette instead.  

> *"[I have approximate knowledge of many things](https://youtu.be/W9_iQ1FSnp8?t=33s) ..."*

This idea actually occurred to me very early on in the contest, and my plan was to use it as a backup solver for problems too difficult for my exact solver.  Well, as it was now turning out to the be case, that would be nearly all of them.  I figured most problems would be *approximately* convex, and so this solver would at least produce reasonably high resemblances.  This simple solver would even create perfect solutions (albeit extremely ugly ones, in physical reality) if the silhouette happened to be convex.  Hoping against hope, I crunched the numbers and found a full one-third of contest problems were, in fact, convex shapes.  So it wasn't as bad as a plan as I feared.

In the final hours of the contest, I had abandoned any thought of doing origami at all; I was instead focused completely on leaderboard hacking.  I was going to find all convex-hull problems and refine my program until I had driven them to zero, or as close as I could possibly get.  In the end, I was able to solve all but 64 such problems.  The program worked decently on non-convex problems as well, but never finding perfect matches.  (One aborted idea was to look for problems that would *become* convex with a single flip -- imagine a thin rectangle, folded into a V -- but I ran out of time before I could implement it).

##Estimated Results

Using the latest snapshot (one hour from the end of the contest), I estimate I earned 20215 points from the 36 problems I submitted.  (Average size of a problem was 3100 bytes).  Of those, 28 were perfectly solved by 3 teams or fewer.  I estimate I could have earned about 5600 more points from the ten problems I neglected to submit.

I also estimate that I earned about 15150 points from the 990 perfect solutions, and 10659 points from the imperfectly solved ones (the organizers didn't exactly specify how points would be awarded 'proportional to the resemblance of [the] solution', so this figure assumes I got an average share for each problem). I'm somewhat surprised by how many points could be earned by imperfect solutions, as I didn't realize that there would be so many problems that were perfectly solved by only a few teams, leaving a sizable share to be divided up amongst the imperfect solvers.  Then again, this is the category of points most likely to be revised downwards due to the results of the last hour of the contest, as it is vulnerable to both perfect and imperfect solutions being submitted by other teams.
