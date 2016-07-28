
This is an ASCOM ObservingConditions Driver for the Boltwood CloudSensor instrument.

The driver is based on the ClarityII "Single Line Data File Facility" (new format),
as described in Section 17.1 of the the "Cloud Sensor II User’s Manual" (Tuesday, 
August 21, 2012), from http://www.cyanogen.com.

The ClarityII software can be configured to save the data gathered from the sensors in an
external file which is updated every two seconds.  The driver needs to be configured (from 
the setup dialog) with the path to such a file (either produced locally or mapped from
another computer).  It will analize the file's contents according to the format described
in the manual and provide its contents via an ASCOM ObservingConditions interface.


Author: Arie Blumenzweig <blumzi@013.net>
Owner:  The Astrophysics Department, Tel Aviv University (July 2016)
