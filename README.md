# BetterStayOnline

We all hate pesky ISPs out there that use common tricks to limit your bandwidth, BUT NO MORE! Better Stay Online is a free tool to help you monitor your bandwidth over time and make sure you Better Stay Online!

This project came about when I was stuck in a bad contract with my ISP who was continuously cutting my bandwidth speeds. After one month, they cut it by 20%. Then after another two months they did that again. And without my noticing they cut it again to just 13mbps, which in the world of streaming services is just unusable. I created a very basic version of this tool to monitor my speeds by performing an Ookla Speedtest (speedtest.net the most trusted bandwidth tester by most ISPs at least in the UK) every time I turned on my PC. Over time I built up proof that they were cutting my speeds and brought it to them. Aftesr being hung up on by many of their customer service staff who didn't want to deal with me, finally I got through to a manager and they let me out of the contract with no early termination fees.

So I decided to build a more user friendly tool that could hopefully help more out there with the same problems. That's where Better Stay Online comes in. By default BSO does an Ookla Speedtest every time you turn on your PC, but you can choose to turn this off and do them manually. You can also create scheduled tests on days and times of your choice. It can be a common trick for ISPs to limit your bandwidth during peak times, so scheduling tests at these times can be really helpful. You can even set a minimum expected speed (for download and upload) for when ISPs give you a "guaranteed speed", so you can check if and how often they break this promise.

I hope someone out there gets some use of this tool and saves some money on bad ISP contracts and gets a better deal because that's what it did for me. 

This project is in it's infancy still and what we need is more useful features for anyone out there who might use it. That means more settings, features, maybe more things to test (people have suggested ping tests to local servers for mobile internet). 

To help out go to the CONTRIBUTING_GUIDELINES file where you can find guidelines for getting involved and submitted features requests and support tickets.

This is a C# WPF app built using the MVVM structure. The graphing tool used is ScottPlot. 

[Google Drive link for install](https://drive.google.com/drive/folders/1Zxl6au1OMKNHPwOTp7yNSWBwyJETbk0K?usp=share_link)
Be aware! This is not a signed executable file! Windows Defender will flag it because of that. If you want peace of mind and are able to, I would prefer you took the code and built it yourself. This is a link is just for those that can't!
