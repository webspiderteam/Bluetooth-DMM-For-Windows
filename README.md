# Bluetooth DMM for Windows 

Bluetooth DMM for Windows is an application for Bluetooth Dmm Multimeter devices to connect and get datas over Gatt. Project is builded over BluetootLE Explorer for connection ability and changed for Dmm devices to reading characteristic values by decoding. Application is not made for any financial think. Just for Personal NEEDS .

I'm not a pro at C# but I have experience about Programming and I haven't found any app for windows that can communicate with my Aneng AN9002 Multimeter but thankfully i found [this source](https://github.com/ludwich66/Bluetooth-DMM/wiki/Bluetooth-DMM-11-Byte-Data-Protocol) and I tried to make it and I also found BluetoothLE Explorer Sources then I modified it and added some decoding codes. So here it is. 

As I said before I'm not pro and also most of BluetoothLe Explorer codes and sources still in here. I just added what i need and hided what I don't need. So they are all there. I haven't cleaned them because some of them is needed somewhere that i didn't realize and the other reason is this is my first experience on Wpf and also UWP and I really can't figured out the structure compeletely. 

For this reasons probably there will not any Update on this project but if there is succestions by you over this project I will try to make them. If you have issues with code I also tyr to fix but I'm not sure I can... and one last thing, as I said before I made this project with my Aneng an9002 and Im not sure its compilable with other same type models. If its not I could try to integrate them by your helps.

New Alpha Release is in WPF type and added 10byte protocol that Aneng V05B uses and also another 10byte protocol that Aneng ST207 uses.
You can download it from releases.

Link is here that compiled  [https://github.com/webspiderteam/Bluetooth-DMM-For-Windows/releases](https://github.com/webspiderteam/Bluetooth-DMM-For-Windows/releases/latest)

Detailed Info: https://github.com/webspiderteam/Bluetooth-DMM-For-Windows/wiki  CHANGE 22-NOV-2022

# Screenshots :

![Bluetooth DMM for Windows Main Window with Menu](https://user-images.githubusercontent.com/85828505/181203401-349d8c22-837f-4a36-883f-41ca2881b631.png)

![Bluetooth DMM for Windows Main Window](https://user-images.githubusercontent.com/85828505/180448743-e0200384-13cc-494a-a210-c1c77f57c419.png)

![Bluetooth DMM for Windows Connection Window](https://user-images.githubusercontent.com/85828505/180449524-7202afc3-8935-4c3a-a38c-cc5b02f3ba2a.png)

![Bluetooth DMM for Windows Connection request](https://user-images.githubusercontent.com/85828505/180449697-e0b7e208-c608-40ab-8d4a-9258311977f9.png)

![Bluetooth DMM for Windows Settings Window](https://user-images.githubusercontent.com/85828505/181202448-8d710fe5-bc52-4046-b418-0fc60a80e6b2.png)

![Bluetooth DMM for Windows Settings Window Languages](https://user-images.githubusercontent.com/85828505/181203070-85a094d1-33c7-4d5a-8a5a-3aa47cd9a347.png)

![Bluetooth DMM for Windows Settings Window in Deutsch](https://user-images.githubusercontent.com/85828505/181203840-f2448a41-eab6-452a-9e67-062e95ee8af6.png)

![Bluetooth DMM for Windows Device Manager](https://user-images.githubusercontent.com/85828505/180448323-efef630b-3aa8-4300-9195-7e7ce0ec2185.png)

![Bluetooth DMM for Windows Device Manager Renamed Device](https://user-images.githubusercontent.com/85828505/180448485-4bce6f51-190c-4948-8c3e-e679b4ebb7b1.png)

# Supported Devices :

Aneng AN9002, BSIDE ZT-300AB, ZOYI ZT-300AB, BABATools AD900

Aneng V05B, BSIDE ZT-5B, ZT-5B

Aneng ST207, BSIDE ZT-5BQ, ZT-5566B, ZT-XB

Aneng AN999S, ZOYI ZT-5566S


# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/) and
This project has adopted the [BluetoothLE Explorer](https://github.com/microsoft/BluetoothLEExplorer).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

Note: **[Bluetooth Explorer](https://www.ellisys.com/products/bex400)** by Ellisys is a separate unaffiliated product.
