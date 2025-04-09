# Fuzzy Logic Controller Project
This is a FLC project built in Unity for a University assignment.
This repo contains the custom code used, but exludes the other Unity assests such as Materials, Prefabs, Scenes, and Settings to make it clearer.

# What Is Its Purpose
This FLC controls a grass cutter robot, encouraging it to cut as much grass as possible whilst avoiding obstacles in the environment

# How It Works
The project is built in such a way that the grass, robot, and obstacles can be spawned in, which was supposed to help drive a playground interface in a web browser, but the project did not get to that stage. So instead it spwans in a singular predictable fashion, but this can always be edited manually in the [code](./Assets/Scripts/World%20Setup.cs)

# Code Explaination
- [World Setup](./Assets/Scripts/World%20Setup.cs) spawns the initial objects into the world, e.g. the robotic lawnmower and grass objects
- [Grass Controller](./Assets/Scripts/Grass%20Controller.cs) handles the outcomes of collisions for the grass for when something collides with them (either the lawnmower or an obstacle)
- [Lawnmower Controller](./Assets/Scripts/Lawnmower%20Controller.cs) is the main FLC body of this repo, and holds all the logic for the FLC to work (including Membership Functions, Thresholds, Rules, etc...)
