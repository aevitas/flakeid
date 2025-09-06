![Logo](./assets/snowflake-96.png)

# Flake ID

Snowflake IDs were originally introduced by Twitter in 2010 as unique, decentralized IDs for Tweets. Their 8-byte size, ordered nature and guaranteed uniqueness make them ideal to use as resource identifiers. Since then, many applications at various scale have adopted Snowflake-esque identifiers.

This repository contains an implementation of decentralized, K-ordered Snowflake IDs based on [the Discord Snowflake specification](https://discord.com/developers/docs/reference). The implementation heavily focuses on high-throughput, supporting upwards of 10.000 unique generations per second on commodity hardware (up to the theoretical limit of around 4 million per second).

You can grab the latest stable version from NuGet:

```
Install-Package FlakeId
```

## Generating IDs

The package revolves around an `Id` struct, with several extensions for convenience. To create a new &mdash; guaranteed unique &mdash; ID:

```csharp
long id = Id.Create();
```

Every `Id` is implicitly convertible to a `long`, sortable, and a natural fit for database ID columns.


# Anatomy

Every Snowflake fits in a 64-bit integer, consisting of various components that make it unique across generations.
The layout of the components that comprise a snowflake can be expressed as:

```
Timestamp                                   Thread Proc  Increment
111111111111111111111111111111111111111111  11111  11111 111111111111
64                                          22     17    12          0
```

The Timestamp component is represented as the milliseconds since the first second of 2015 (the default **epoch**). Since we're using all 64 bits available, this epoch can be any point in time, as long as it's in the past. If the epoch is set to a point in time in the future, it may result in negative snowflakes being generated.

Where the original Discord reference mentions worker ID and process ID, we substitute these with the
thread and process ID respectively, as the combination of these two provide sufficient uniqueness, and they are
the closest we can get to the original specification within the .NET ecosystem.

# Epoch

The timestamp component is a delta from a predefined instant in time, this instant is known as the **epoch**.

The default epoch is `01/01/2015 +0`. The implementation will generate valid IDs for about 139 years, after which they will start to roll over.

While it is fine to modify the code to choose a different epoch for your specific use case, you should always take care to only use a single epoch per domain, as modifying it afterwards might lead to collisions.

## Timestamps

Because every Snowflake contains 42 bits of timestamp information, it is possible to convert a Snowflake into a timestamp. 

FlakeId provides two extension methods:

```
    DateTimeOffset createdAt = id.ToDateTimeOffset();
    long createdAtUnixMilliseconds = id.ToUnixTimeMilliseconds();
```

## Why create FlakeId?

To put is simply, because all other available libraries at the time of writing created either 128-bit integers, or weren't performing very well. We strongly believe that a fundamental piece of code such as an ID generator should do its job out of the box, while being extremely efficient.

## Web Clients

⚠️⚠️⚠️
> Be careful when exposing IDs to JavaScript and Node clients. Most JS engines are limited to 56 bit floating point numbers. This may lead to IDs having their last 8 bits truncated, e.g.: `931124405369716748` might become `931124405369716700` when interpreted by a JS client.

When exposing your IDs to web clients, it is recommended to use the `ToBase64String()` extension and having your client interpret the ID as a `string`.  

## Performance

We've benchmarked FlakeId on .NET 8 against [IdGen](https://github.com/RobThree/IdGen), which is another implementation of Snowflake IDs in .NET. FlakeId performs significantly better.


```
BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3155/23H2/2023Update/SunValley3)
AMD Ryzen 5 5600X, 1 CPU, 12 logical and 6 physical cores
.NET SDK 8.0.201
  [Host]     : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX2


| Method         | Mean        | Error     | StdDev     | Code Size |
|--------------- |------------:|----------:|-----------:|----------:|
| Single_FlakeId |    349.2 ns | 6.58 ns   |  6.16 ns   |     358 B |
| Single_IdGen   | 3,473.96 ns | 69.295 ns | 168.673 ns |     671 B |
```

In this benchmark, IdGen was configured to `SpinWait` in the event multiple IDs were generated in the same instant. It spent most of its time in a spinlock.

Below are the benchmark results for FlakeId running on multiple runtimes.

```
BenchmarkDotNet v0.13.12, macOS 15.6.1 (24G90) [Darwin 24.6.0]
Apple M1 Pro, 1 CPU, 10 logical and 10 physical cores
.NET SDK 9.0.100
  [Host]   : .NET 8.0.2 (8.0.224.6711), Arm64 RyuJIT AdvSIMD
  .NET 7.0 : .NET 7.0.11 (7.0.1123.42427), Arm64 RyuJIT AdvSIMD
  .NET 8.0 : .NET 8.0.2 (8.0.224.6711), Arm64 RyuJIT AdvSIMD
  .NET 9.0 : .NET 9.0.0 (9.0.24.52809), Arm64 RyuJIT AdvSIMD


| Method         | Job      | Runtime  | Mean     | Error   | StdDev   |
|--------------- |--------- |--------- |---------:|--------:|---------:|
| Single_FlakeId | .NET 7.0 | .NET 7.0 | 354.0 ns | 6.87 ns | 13.89 ns |
| Single_FlakeId | .NET 8.0 | .NET 8.0 | 356.6 ns | 7.17 ns |  9.81 ns |
| Single_FlakeId | .NET 9.0 | .NET 9.0 | 349.2 ns | 6.58 ns |  6.16 ns |

```

## Issues

If you find any issues when using FlakeId, please open an issue and describe your problem as accurately as you can.

## Contributions

Pull Requests are always welcome. When contributing, please follow the naming conventions and coding style as described in the `.editorconfig` file. Should you consider a sizable change, please open an issue beforehand so that your change can be openly discussed.
