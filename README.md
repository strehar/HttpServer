# HttpServer
##Simple HTTP Server library for windows IoT devices.

There is limited HTTP server support functionality in Windows IoT, but some projects could really benifit from embedded HTTP Server. It specially makes it easy to display data to the clients or controll the IoT device itself.

This HTTP server library can be embedded in any project and is simple to use. User does not need to know anything about HTTP protocol or what is going on behind the scenes. It is all done with few calls to functions of HttpServer class.

For example, to embed simple HelloWorld style web page in your application, you would add following code:

```
using feri.MS.Http;

...

HttpServer server = new HttpServer();
server.start();
```

This will start http server on port 8000 and display "It works!" page to user.

To add some functionality to the server, you would have to write and register listener function. When registring listener, you need to provide URL that will tell the server when to trigger event.

Code could look something like this:

```
using feri.MS.Http;

...

HttpServer server = new HttpServer();
server.AddPath("/HelloWorld.html", HelloWorldListener);
server.start();

...

public void HelloWorldListener(HttpRequest request, HttpResponse response) {
    // Do some work here
}
```

The listener method is provided HttpRequest and HttpResponse objects.

HttpRequest contains all information about user request (from where connection came, what method it was, what was url, what were parameters, what were http headers, cookies, session related to the user, what was data, ...). HttpResponse takes care of sending data back to user (sending data, headers, cookies).



If you wish to limit access to server to some users, server supports Basic HTTP authentication.

```
using feri.MS.Http;

...

HttpServer server = new HttpServer();
server.AddUser("user","password");
server.AuthenticationRequired = true;
server.AddPath("/HelloWorld.html", HelloWorldListener);
server.start();

...

```

Sometimes you need some periodic data, for example reading temperature from sensors. To do this you could register new timer, that will get called periodically.

```
using feri.MS.Http;

...

HttpServer server = new HttpServer();
...
AddTimer("TimerName", 10000, TimerListener);

...

public void TimerListener() {
    // Do some work
}
```

Timers are not passed any information, since they are not related to any HTTP request or response.

For more information and example of use, please view the included demo project. It shows how to use tings like simple templating engine, JSON, classes for some electronic parts, ability to define server root path to serve static content, using JavaScript to display JSON data, ...
