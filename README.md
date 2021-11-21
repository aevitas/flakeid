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

## Performance

We've benchmarked FlakeId on .NET 5 against [MassTransit's NewId](https://github.com/phatboyg/NewId) library, and [IdGen](https://github.com/RobThree/IdGen) both libraries are widely used. It is worth noting that NewId generates 128-bit integers.

We've also included `Guid.NewGuid` as a baseline benchmark, as it is very well optimized, and arguably the most widely used identifier generator in .NET.

```
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i7-8700K CPU 3.70GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=5.0.201
  [Host]     : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
  DefaultJob : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT


|         Method |        Mean |      Error |     StdDev | Code Size |
|--------------- |------------:|-----------:|-----------:|----------:|
| Single_FlakeId |    30.44 ns |   0.091 ns |   0.080 ns |     254 B |
|    Single_Guid |    59.47 ns |   0.681 ns |   0.637 ns |     111 B |
|   Single_NewId |    75.27 ns |   0.323 ns |   0.270 ns |      40 B |
|   Single_IdGen | 2,445.98 ns | 176.372 ns | 520.036 ns |     687 B |
```

In this benchmark, IdGen was configured to `SpinWait` in the event multiple IDs were generated in the same instant. It spent most of its time in a spinlock.

## Issues

If you have an issue with FlakeId, please open an issue and describe your problem as accurately as you can.

## Contributions

Pull Requests are always welcome. When contributing, please follow the naming conventions and coding style as described in the `.editorconfig` file. Should you consider a sizable change, please open an issue beforehand so that your change can be openly discussed.
