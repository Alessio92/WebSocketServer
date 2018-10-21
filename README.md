# WebSocketServer
 
This project is a WebSocket server in dotnet core, so can be compiled in all supported platform.  
I've implemented the standard [rfc6455](https://tools.ietf.org/html/rfc6455).

## Supported Feature (OpCodes)

* Text
* Ping
* Pong
* Close Connection

## Not Supported Feature

* Binary

I've choose to don't support binary because I need to use only json string in my projects

## Compile

* **Windows x64**

    `dotnet publish -r win-x64`

* **Ubuntu 18.04 x64**

    `dotnet publish -r ubuntu.18.04-x64`

* **Mac OSX x64**

    `dotnet publish -r ubuntu.18.04-x64`

* **Other**, [dotnet rids](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog)

## Test

Use the *index.html* file in *Client* folder.

## Known Issues

*	*131072 byte limit*

During development i've found a bug in clients implementation (aka all my browsers) that miscalculate the packet size if size is > than 131072, you can test this bug [in official websocket echo service](https://websocket.org/echo.html) by sending 131073 length string, you will see the connection close by server, so I just copied this behavior

## Authors

* **Alessio Carello** - [Alessio92](https://github.com/Alessio92)

## License

This project is licensed under the MIT License.