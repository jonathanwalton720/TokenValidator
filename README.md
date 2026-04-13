# Token Validator

This repo demonstrates a C# class library that can be pushed to nuget and shared across projects. What makes this different from a normal OAuth solution is the addition of a custom property, a LogoutTime that a central source, a .NET Web API, for instance, will need to validate, incase the user has logged out from a different application. There is caching for the credentials to alleviate the need to the increased web traffic the validation of the property would cause to a central OAuth authority that validates the LogoutTime using the supplied library.

## Getting Started

### Dependencies

* Nuget
* JWT Authentication authroity for Single SIgn On from a Web API

### Installing

* This project is incomplete, and is just an example.

### Executing program

* How to run the program
* Step-by-step bullets
```
code blocks for commands
```

## Help

Any advise for common problems or issues.
```
command to run if program contains helper info
```

## Authors

Contributors names and contact info

ex. Jonathan Walton
ex. https://www.github.com/jonathanwalton720
