FSActiveFires
=============

![Screenshot of FS Active Fires](http://i.imgur.com/AQDqF2L.png)

A utility that downloads MODIS active fire data from the [NASA FIRMS website](https://earthdata.nasa.gov/firms) to create fires in Microsoft Flight Simulator.

![Regions](https://earthdata.nasa.gov/sites/default/files/styles/large/public/null/Regions_500px.jpg)

Region image from the [NASA website](https://earthdata.nasa.gov/data/near-real-time-data/firms/active-fire-data).

Frequently Asked Questions
---

Q: What does fires in simulation mean?  Why doesn't it match the total fires downloaded?

A: Fires in simulation refers to the number of fires FS Active Fires currently has placed in the simulator.  The number of fires in the simulation will change depending on where the user aircraft is.  The number of total fires per region changes depending on how many hotspots the MODIS instrument aboard the Aqua and Terra satellites detect.

Q: What does the minimum confidence slider do?

A: FS Active Fires will only place fires in the simulator if the detection confidence of the fire is greater than or equal to the value selected using the slider.

Q: What does the detection confidence of the fire represent?

A: See the [FIRMS FAQ](https://earthdata.nasa.gov/data/near-real-time-data/faq/firms) or [About page](https://earthdata.nasa.gov/data/near-real-time-data/firms/about).  Relevant sections quoted below:

> [**What is the detection confidence?**](https://earthdata.nasa.gov/data/near-real-time-data/faq/firms#firms23)

> A detection confidence is intended to help users gauge the quality of individual active fire pixels. This confidence estimate, which ranges between 0% and 100%, is used to assign one of the three fire classes (low-confidence fire, nominal-confidence fire, or high-confidence fire) to all fire pixels within the fire mask. The confidence field should be used with caution; it is likely that it will vary in meaning in different parts of the world. Nevertheless some of our end users have found such a field to be useful in excluding false positive occurrences of fire.

> This value is based on a collection of intermediate algorithm quantities used in the detection process. It is intended to help users gauge the quality of individual hotspot/fire pixels. Confidence estimates range between 0 and 100% and are assigned one of the three fire classes (low-confidence fire, nominal-confidence fire, or high-confidence fire).

Usage
---

If it's your first time running FS Active Fires, click Install Model to install the fire effect model.

Select a region and adjust the minimum confidence slider, then download data at your discretion.  Once Flight Simulator is running, click the Connect button.  Fires should appear as you approach them.  Alternatively, you can optionally click Move User to Random Fires to move your aircraft to 3000 feet MSL above a random fire.

Additionally, there are command line arguments to automatically download data, set the confidence level, and connect to Flight Simulator on startup.  Example usage below:

    FSActiveFires.exe -confidence:70 -download:"U.S.A. (Conterminous) and Hawaii" -connect

Confidence supports integers on the interval [0,100].  Download is case sensitive and supports the same entries you see in the drop down.

Here's an example exe.xml if you want to automatically launch FS Active Fires with FSX:

    <SimBase.Document Type="Launch" version="1,0">
      <Descr>Launch</Descr>
      <Filename>exe.xml</Filename>
      <Disabled>False</Disabled>
      <Launch.ManualLoad>False</Launch.ManualLoad>
      <Launch.Addon>
        <Name>FSActiveFires</Name>
        <Disabled>False</Disabled>
        <ManualLoad>False</ManualLoad>
        <Path>REPLACE WITH PATH TO FSActiveFires.exe</Path>
        <CommandLine>-confidence:70 -download:"U.S.A. (Conterminous) and Hawaii" -connect</CommandLine>
      </Launch.Addon>
    </SimBase.Document>

Credits & Acknowledgements
---
- Brandon Filer   - Creating the model that emits the fire effect and creating the icon for the program.
- Steven Frost    - Creating the ini parsing class used in the program.
- Tim Gregson     - Creating the BeatlesBlog Managed SimConnect library.
- Dean Mountford  - Publishing [the thread at FSDeveloper](http://www.fsdeveloper.com/forum/threads/global-wildfires-open-source-project-need-programming.428525/) requesting a utility like this.

[ESRI Shapefile Reader from CodePlex.](https://shapefile.codeplex.com/)

We acknowledge the use of FIRMS data and imagery from the Land Atmosphere Near-real time Capability for EOS (LANCE) system operated by the NASA/GSFC/Earth Science Data and Information System (ESDIS) with funding provided by NASA/HQ.

NASA FIRMS NRT MODIS Near real-time Hotspot / Active Fire Detections [MCD14DL](https://earthdata.nasa.gov/node/5322) data set. Available on-line [https://earthdata.nasa.gov/firms].
