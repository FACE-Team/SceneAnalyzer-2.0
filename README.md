# SceneAnalyzer ver.2.0
##### Copyright FACE Team, Research Center E. Piaggio, Univeristy of Pisa 2016 www.faceteam.it

Scene Analyzer is a unique perception system able to detect and analyse a wide range of human and environment relevant features with the awareness of real-world contents, and compatible with different robotics platforms. As shown in Figure below, SA has several standard sub-modules and layers which allows researchers in HRI area to perform complex perception tasks with a minimum effort just by using SA as a perfect and unique out-of-the-box perception toolkit.   

![Alt text](/SAdiagram.png?raw=true "SceneAnalyzer Architecture")



#NEW TECHNICAL FEATURES 

The new technical features presented in SA 2.0 are as follows: 
 * Modularity: SA 2.0  has a modular structure that allows developer to decompose the perception system into a number of components that can be mixed and matched in a variety of configurations, in order to adapt the system to a specific robotic framework.
 * Interconnectivity: the system components have the capabilities to connect, interact, or exchange resources and information in some ways, in order to infer and deliver the real-world content rather than only detect features.
 * Extendability: it allows extending perception system by easy adding/removing or replacing system components and the corresponding software libraries without affecting other modules.
 * Communication and platform independent: it allows perception system to transfer all the extracted perceptual information to other machines running in different operation system.

[![Alt text for your video](http://imgur.com/2oDiCsx)](https://www.youtube.com/watch?v=px3nTTn6sUw)

# REQUIREMENTS:
- visual Studio 2012
- .NET v4.5
- Yarp v2.3.63 [PRE-COMPILE](https://sourceforge.net/projects/yarp0/files/yarp2/yarp-2.3.63/windows/yarp_2.3.63_v11_x86_0.exe/download)
- Kinect XBOX ONE for Windows and related drivers
- Kinect SDK v2.0
- MySql Connect 6.9.9 [link](http://dev.mysql.com/downloads/file/?id=463758)
- Microsoft Speech [link](https://www.microsoft.com/en-us/download/confirmation.aspx?id=27225)
- Microsoft Expression Blend [link](https://www.microsoft.com/it-it/download/confirmation.aspx?id=10801)

###NOTE ABOUT USB3.0 CONTROLLERS FOR KINECT
http://support.xbox.com/en-us/xbox-on-windows/accessories/kinect-for-windows-v2-known-issues
https://social.msdn.microsoft.com/Forums/it-IT/bb379e8b-4258-40d6-92e4-56dd95d7b0bb/confirmed-list-of-usb-30-pcie-cardslaptopsconfigurations-which-work-for-kinect-v2-during?forum=kinectv2sdk

#COMPARISON BETWEEN VERSIONS
In this new release the most important change is modularity: each module of SceneAnalyzer (i.e. SHORE, Saliency, SubjectRecognitionQRCode) is indipendent and it can be individually executed. 
Also the XML file which describes the metascene has a new structure. An example of the new structure, with detalied comments, is provided as scene.XML.

# HOW TO START

Then compile the FACE Tools project and DataBaseManager Project

# TROUBLESHOOTING

Please ensure that you are using the English (UK) Format in 'Region and Language' from Control Panel, otherwise signs and points for decimals will disappear from the data provided by the SA.
In our tests we experienced several difficulties using virtual machines. Therefore, if possible, don’t use them. There are problems with Windows libraries of the Kinect and the functioning is not guaranteed.



# ID ASSIGNMENT

The assignment of ID and name to detected subjects in the scene is managed by the SubjectRecognitionQRCode module. This module can be activated from the SceneAnalyzer control panel.
The SubjectRecognitionQRCode module read the QRcode present on the body of subjects and queries a MySQL database through the DatabaseManager module: if the subject is not already present in the database, the QR module opens a pop-up window asking for the name of the new detected subject; 15 secs is the time limit for digiting the information since the moment in which the subject is detected, if no information is provided within the expected time the pop-up window will end automatically without interrupting the smooth functioning of the program.

#FACIAL FEATURES ANALYSIS MODULE

Information about the emotional state of the subjects is estimated through facial expressions analysis performed through the sophisticated high-speed object recognition engine called SHORE. For more information please refere to the references below: 

Fraunhofer Institute website (http://www.fraunhofer.de/en.html)

C. Küblbeck, and E. Andreas. "Face detection and tracking in video sequences using the modifiedcensus transformation." Image and Vision Computing 24.6 (2006): 564-572.


###NOTE ABOUT SHORE LICENSE
The software can be licensed based on individual or bulk license contracts. The University of Pisa has a Software Evaluation License agreement with Fraunhofer Institute  for the employment of SHORE™ software. 
In order to be used, each user will have to directly ask for the license from the same institute. 


# YARP PORTS

 - /SceneAnalyzer/MetaSceneXML:o (in this port we stream the Metascene as an XML file deserialized in a string)
 - /SceneAnalyzer/MetaSceneOPC:o (in this port we stream the Metascene as an OPC format string)
 - /SceneAnalyzer/Sentence:i
 - /SceneAnalyzer/LookAt:i

[the name of these ports can be modified, and new ports can be instantiated, in the 'app.config' whithin the SceneAnalyzer Project]

# ScreenShot of SA 2.0
![Alt text](/SAEA.png?raw=true "SceneAnalyzer Enviromental Analysis")
![Alt text](/SA_subjectAnalysis.png?raw=true "SceneAnalyzer Subject Analysis")


#PUBLICATIONS

@inproceedings{mazzei2012hefes,
  title={Hefes: An hybrid engine for facial expressions synthesis to control human-like androids and avatars},
  author={Mazzei, Daniele and Lazzeri, Nicole and Hanson, David and De Rossi, Danilo},
  booktitle={Biomedical Robotics and Biomechatronics (BioRob), 2012 4th IEEE RAS \& EMBS International Conference on},
  pages={195--200},
  year={2012},
  organization={IEEE}
}

@article{Lazzeri2013393,
	title = {Towards a believable social robot},
	author = {Lazzeri, N., Mazzei, D., Zaraki, A., De Rossi, D.},
	url = {http://www.scopus.com/inward/record.url?eid=2-s2.0-84880712333&partnerID=40&md5=724a58c54727725eff32f0338306f45e},
	year = {2013},
	date = {2013-01-01},
	journal = {Lecture Notes in Computer Science (including subseries Lecture Notes in Artificial Intelligence and Lecture Notes in Bioinformatics)},
	volume = {8064 LNAI},
	pages = {393-395},
	note = {cited By 1},
	pubstate = {published},
	tppubtype = {article}
}

@article{zaraki2014designing,
  title={Designing and evaluating a social gaze-control system for a humanoid robot},
  author={Zaraki, Aolfazl and Mazzei, Daniele and Giuliani, Manuel and De Rossi, Danilo},
  journal={Human-Machine Systems, IEEE Transactions on},
  volume={44},
  number={2},
  pages={157--168},
  year={2014},
  publisher={IEEE}
}

# LICENSE
In order to make this code usable in closed source not-commercial applications under specified conditions, this code is licensed under GPL 3 with linking exception (http://www.gnu.org/licenses/gcc-exception-3.1.en.html).
The exception dosen't permit the use of this code for commercial applications also in form of executable or binary code. 
This exception is also applied if you wish to combine this code with a proprietary code necessary for the running of a product.
Moreover, this exception specifies the details of the claiming necessary for the use of this code.
Any work based, or that links, this code need to claim the FACE Team by using the link www.faceteam.it
The following scientifc publications need also to be cited in all the scientific/accademic publications where this or any derived code is used.


For further information please feel free to contact us at info@faceteam.it
