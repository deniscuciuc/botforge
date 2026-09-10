# BotForge.Analyzers

Roslyn analyzers and code fixes for [BotForge](https://github.com/deniscuciuc/botforge).
They catch the mistakes the compiler cannot: a handler with the wrong return type, a
duplicate route, a handler class missing its routing attribute.

```
dotnet add package BotForge.Analyzers
```

It is a development-time dependency and adds nothing to your published output.

Licensed under the [MIT License](https://github.com/deniscuciuc/botforge/blob/main/LICENSE).
