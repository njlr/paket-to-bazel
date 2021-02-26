load("@io_bazel_rules_dotnet//dotnet:defs.bzl", "fsharp_binary")

fsharp_binary(
  name = "paket_to_bazel.exe",
  srcs = [
    "Program.fs",
  ],
  deps = [
    "@core_sdk_stdlib//:libraryset",
    "@fsharp.core//:lib",
    "@fsharpx.extras//:lib",
    "@fstoolkit.errorhandling//:lib",
    "@argu//:lib",
    "@paket.core//:lib",
    "@thoth.json//:lib",
    "@thoth.json.net//:lib",
  ],
)
