<img src="assets/NSS-128x128.png" align="right" />

# Nefarius.Peripherals.SerialPort

`System.IO.Ports.SerialPort` but actually works 😏

## About

Originally copied from [John Hind - "Use P/Invoke to Develop a .NET Base Class Library for Serial Device Communications"](http://msdn.microsoft.com/en-us/magazine/cc301786.aspx) that I guess licensed under Ms-PL so this project is also under Ms-PL. (Update: well, after the years now I think this was not a true claim, but well I don't think MS will sue anyone because a sample intended for public use)

It is useful in the cases System.IO.Ports.SerialPort is not working well (for connecting to \\\\.\\... devices)

## Motivation behind this fork

`System.IO.Ports.SerialPort` is terrible and [this is exactly what I've experienced in a project](https://www.sparxeng.com/blog/software/must-use-net-system-io-ports-serialport) so this library came to the rescue.

## Download

Consume the NuGet via `Install-Package Nefarius.Peripherals.SerialPort`
