![Logo](./assets/snowflake-96.png)

# Flake ID

Snowflake IDs were originally introduced by Twitter in 2010 as unique, decentralized IDs for Tweets. Their 8-byte size, ordered nature and guaranteed uniqueness make them ideal to use as resource identifiers. Since then, many applications at various scale have adopted Snowflake-esque identifiers.

This repository contains an implementation of decentralized, K-ordered Snowflake IDs based on [the Discord Snowflake specification](https://discord.com/developers/docs/reference). The implementation heavily focuses on high-throughput, supporting upwards of 10.000 unique generations per second on commodity hardware.

You can grab the latest stable version from NuGet:

```
Install-Package FlakeId
```

## How it works

Every Snowflake fits in a 64-bit integer, consisting of various components that make it unique across generations.
The layout of the components that comprise a snowflake can be expressed as:

```
Timestamp                                   Thread Proc  Increment
111111111111111111111111111111111111111111  11111  11111 111111111111
64                                          22     17    12          0
```

The Timestamp component is represented as the milliseconds since the first second of 2015. Since we're using all 64 bits available, this epoch can be any point in time, as long as it's in the past. If the epoch is set to a point in time in the future, it may result in negative snowflakes being generated.

Where the original Discord reference mentions worker ID and process ID, we substitute these with the
thread and process ID respectively, as the combination of these two provide sufficient uniqueness, and they are
the closest we can get to the original specification within the .NET ecosystem.

The Increment component is a monotonically incrementing number, which is incremented every time a snowflake is generated.
This is in contrast with some other flake-ish implementations, which only increment the counter any time a snowflake is 
generated twice at the exact same instant in time. We believe Discord's implementation is more correct here,
as even two snowflakes that are generated at the exact same point in time will not be identical, because of their increments.

## Usage

FlakeId revolves around a single type: `Id`. This type effectively embodies a `long`, and can be stored and used anywhere a signed, 8-byte integer is used.

Creating a Snowflake is simple:

```
    long id = Id.Create();
    Id id = Id.Create();
```

Every `Id` is implicitly convertable to `long`, which means that you don't have to make any changes to your types if you want to start using FlakeId, assuming they are already using `long` IDs. Conversely, every `long` can be represented as an `Id` by constructing an ID from it. Do keep in mind that while every Snowflake is a `long`, not every `long` is a Snowflake.

## Timestamps

Because every Snowflake contains 42 bits of timestamp information, it is possible to convert a Snowflake into a timestamp. 

FlakeId provides two extension methods:

```
    DateTimeOffset createdAt = id.ToDateTimeOffset();
    long createdAtUnixMilliseconds = id.ToUnixTimeMilliseconds();
```

## Why create FlakeId?

To put is simply, because all other available libraries at the time of writing created either 128-bit integers, or weren't performing very well. We strongly believe that a fundamental piece of code such as an ID generator should do its job out of the box, while being extremely efficient.

## JavaScript Clients

Be careful when exposing IDs to JavaScript and Node clients. Most JS engines are limited to 56 bit floating point numbers. This may lead to IDs having their last 8 bits truncated, e.g.: `931124405369716748` might become `931124405369716700` when interpreted by a JS client.

There is a `ToStringIdentifier()` extension method available to safely expose IDs to JS clients. This is a Base64 encoded representation of the ID, which can be used by the JS client for subsequent requests. Alternatively, you could also expose your IDs as a `string`, though be careful JS clients recognize this value as a `string`, and not as a `number`.

## Performance

We've benchmarked FlakeId on .NET 8 against [MassTransit's NewId](https://github.com/phatboyg/NewId) library, and [IdGen](https://github.com/RobThree/IdGen) both libraries are widely used. It is worth noting that NewId generates 128-bit integers.

We've also included `Guid.NewGuid` as a baseline benchmark, as it is very well optimized, and arguably the most widely used identifier generator in .NET.

```
BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3155/23H2/2023Update/SunValley3)
AMD Ryzen 5 5600X, 1 CPU, 12 logical and 6 physical cores
.NET SDK 8.0.201
  [Host]     : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX2


| Method         | Mean        | Error     | StdDev     | Code Size |
|--------------- |------------:|----------:|-----------:|----------:|
| Single_FlakeId |    26.48 ns |  0.020 ns |   0.019 ns |     358 B |
| Single_Guid    |    41.85 ns |  0.481 ns |   0.450 ns |     245 B |
| Single_NewId   |    31.83 ns |  0.013 ns |   0.012 ns |     303 B |
| Single_IdGen   | 3,473.96 ns | 69.295 ns | 168.673 ns |     671 B |
```

In this benchmark, IdGen was configured to `SpinWait` in the event multiple IDs were generated in the same instant. It spent most of its time in a spinlock.

Below are the benchmark results for FlakeId running on multiple runtimes.

```
BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3155/23H2/2023Update/SunValley3)
AMD Ryzen 5 5600X, 1 CPU, 12 logical and 6 physical cores
.NET SDK 8.0.201
  [Host]   : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX2
  .NET 5.0 : .NET 5.0.17 (5.0.1722.21314), X64 RyuJIT AVX2
  .NET 6.0 : .NET 6.0.27 (6.0.2724.6912), X64 RyuJIT AVX2
  .NET 7.0 : .NET 7.0.16 (7.0.1624.6629), X64 RyuJIT AVX2
  .NET 8.0 : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX2


| Method         | Job      | Runtime  | Mean     | Error    | StdDev   | Code Size |
|--------------- |--------- |--------- |---------:|---------:|---------:|----------:|
| Single_FlakeId | .NET 5.0 | .NET 5.0 | 27.85 ns | 0.111 ns | 0.103 ns |     254 B |
| Single_FlakeId | .NET 6.0 | .NET 6.0 | 26.37 ns | 0.056 ns | 0.053 ns |     215 B |
| Single_FlakeId | .NET 7.0 | .NET 7.0 | 26.72 ns | 0.211 ns | 0.176 ns |     209 B |
| Single_FlakeId | .NET 8.0 | .NET 8.0 | 26.56 ns | 0.085 ns | 0.071 ns |     358 B |
```

## Issues

If you have an issue with FlakeId, please open an issue and describe your problem as accurately as you can.

## Contributions

Pull Requests are always welcome. When contributing, please follow the naming conventions and coding style as described in the `.editorconfig` file. Should you consider a sizable change, please open an issue beforehand so that your change can be openly discussed.
