# paket-to-bazel

This is a tool that generates [Bazel](https://bazel.build/) scripts from Paket files. This helps you maintain a Bazel build alongside a conventional F# build (Paket and .NET CLI).

Why use Bazel? Well, one big pain-point of F# is build times. If you use Bazel, then build times can be significantly reduced, since Bazel caches can be shared across time and space! It also gives you reproducible builds. This is important if you have continuous deployment, because it prevents deployments being updated when they haven't actually changed.

Currently, the tool does not generate Bazel directly, but instead requires the use of `nuget2bazel`, which was designed for C# projects where Paket is rarely used. In the future this dependency will likely be removed.

## Usage

OK, so how does it work?

First you need a project that uses Paket (such as this very repo!).

Add the tool to your `WORKSPACE` like so:

```python
git_repository(
  name = "paket_to_bazel",
  remote = "https://github.com/njlr/paket-to-bazel",
  branch = "master",
)
```

Next, you need to run the tool against your lock-file:

```bash
bazel run @paket_to_bazel//:paket_to_bazel.exe paket.lock
```

This will generate a `nuget2config.json` file.

Next, you need to use `nuget2bazel` to update the Bazel scripts:

```bash
bazel run @io_bazel_rules_dotnet//tools/nuget2bazel:nuget2bazel.exe -- sync -p $PWD
```

You can then refer to Nuget packages like this:

```python
load("@io_bazel_rules_dotnet//dotnet:defs.bzl", "fsharp_binary")

fsharp_binary(
  name = "app.exe",
  srcs = [
    "Program.fs",
  ],
  deps = [
    "@core_sdk_stdlib//:libraryset",
    "@fsharp.core//:lib",
    "@fsharpx.extras//:lib",
    "@fstoolkit.errorhandling//:lib",
    "@argu//:lib",
    "@thoth.json.net//:lib",
  ],
)
```

What *doesn't* work:

 * Using an alternative Nuget source
 * Using more than one Paket group
 * Using Paket Git and GitHub dependencies
 * Integration with `paket.references` files

## Development

To build using conventional F# tooling:

```bash
dotnet tool restore
dotnet paket restore
dotnet build
```

To build with Bazel:

```bash
bazel build //:paket_to_bazel.exe
```
