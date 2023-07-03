# zbundler

zbundler is another web-assets bundler, but compatible with any kind of project, simpler to setup and easier to use.

What zbundler can do?

- Compile CSS to an minified CSS;
- Compile SCSS/SASS to an minified CSS;
- Compile JavaScript to an minified Javascript.

## Setup

To start using zbundler, after installing in the step below, you create a configuration file in your project root and run the build or watcher.

```json
// zbundler.json
[
    {
        "compilationMode": "CSS",
        "label": "Minify CSS files",
        "include": [
            "./css"
        ],
        "output": [
            "./dist/app.css"
        ]
    },
    {
        "compilationMode": "JS",
        "label": "Minify JS files",
        "include": [
            "./scripts"
        ],
        "output": [
            "./assets/dist.js"
        ],
        "exclude": [
            "/vendor/"
        ]
    }
]
```

And then, you can compile your files using:

```
$ zbundler build zbundler.json
```

That's all!

## Installing zbundler

First, you will need the .NET 7 runtime installed on your machine where you will run zbundler. You can download .NET for your platform [here](https://dotnet.microsoft.com/pt-br/download/dotnet/7.0).

Then, you can download the zbundler latest version in the [Releases](https://github.com/CypherPotato/zbundler) tab.

After downloading it, put somewhere of your preference and add the choosen directory path to your operating system's environment variable so you can quickly access the zbundler executable.

And ready, zbundler is installed. You can start using it in your projects as mentioned in the step above.

## Building zbundler

Just clone and compile this repository with dotnet build or Visual Studio.

## Documentation

The use is so simple that it doesn't need documentation for this project, however, there is a code below explaining how the project's configuration file should be interpreted.

```json
// the configuration file root object must be
// an array.
// it also supports comments.
[
    // this object represents an configuration.
    // configurations are executed in the way
    // they are in this file
    {
        // compilationMode is what you are going to compile in this
        // specific configuration.
        // must be CSS|SCSS|SASS|JS
        "compilationMode": "CSS",

        // label is an display-only text to show in the build/watch
        // process
        "label": "Minify CSS files",

        // here you include everything that will be compiled.
        // it can be an absolute path, relative to the configuration file,
        // a folder or a URL.
        //
        // using folders will always look for files ending in the official
        // ending of the chosen language, and the search is always recursive.
        //  CSS  -> .css
        //  SCSS -> .scss
        //  SASS -> .sass
        //  JS   -> .js
        "include": [
            "../relative/path/to/directory",
            "C:/absolute/path/to/file.css",
            "https://example.com/styles.css"
        ],

        // this is where you define where the compiled files should
        // be written. can be a relative or absolute path to a file.
        // 
        // it will always overwrite the files present in the directory
        // and will create the directories (and sub directories) if they
        // doens't exist.
        "output": [
            "./dist/app.css",
            "./v1/app.css"
        ],

        // exclude patterns define parts that shouldn't be included
        // in file searches.
        // in general, they are applied to absolute paths after they
        // are resolved.
        // 
        // a raw string comparison is done to validate that the path includes
        // the specified part, normalizing both inputs and using an
        // insensitive-case comparison.
        "exclude": [
            "/vendor/",
            ".do-not-include.js"
        ]
    }
]
```

## Credits

- Used [NUglify](https://github.com/trullock/NUglify) on the CSS and JS minifier, which it is an folk of [Ajaxmin](https://github.com/microsoft/ajaxmin).
- Used [dart-sass](https://github.com/sass/dart-sass) binaries to compile SASS/SCSS files.