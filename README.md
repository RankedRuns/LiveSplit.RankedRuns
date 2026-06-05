# LiveSplit Component for RankedRuns.com

https://www.rankedruns.com

## Description

This is the official livesplit integration plugin for rankedruns.com. This plugin offers automatic run submissions to leaderboards, rich presence on your profile and automatic split data uploads. 

## Installation

Build the plugin yourself or download the newest release, put it into your Livesplit -> Component folder, add the component into your layout via Edit Layout -> Add (+) -> Other -> RankedRuns.com and log in via Edit Layout -> Layout Settings -> RankedRuns.com. You will be redirected to our website and authenticated.

## Usage 

For optimal integration, please fill out as much meta data as possible in the "Edit Splits..." -> "Additional Info" tab. This helps the mapper to automatically find categories and variables.

## Building

Get following .dlls from LiveSplit:
  * LiveSplit.Core
  * SpeedrunComSharp
  * UpdateManager

For local testing, make sure to toggle the UseLocalhost = false variable and adjust to your local setup.

IMPORTANT NOTES 2026
To build with Visual Studio 2026:
 * remove project references of 3 .dlls after opening project solution.
 * add .dlls as assembly references
 * keep project targeting .NET framework at 4.8.1!!!