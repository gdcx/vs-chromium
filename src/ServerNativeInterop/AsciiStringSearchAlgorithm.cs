﻿// Copyright 2013 The Chromium Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

using System;
using System.Collections.Generic;
using System.Linq;
using VsChromium.Core.Ipc.TypedMessages;

namespace VsChromium.Server.NativeInterop {
  public abstract class AsciiStringSearchAlgorithm : IDisposable {
    public abstract int PatternLength { get; }
    public abstract int SearchBufferSize { get; }

    public abstract void Search(ref NativeMethods.SearchParams searchParams);

    public virtual void Dispose() {
    }

    private static readonly List<FilePositionSpan> NoResult = new List<FilePositionSpan>();
    /// <summary>
    /// Find all occurrences of the pattern in the text block starting at
    /// |<paramref name="textPtr"/>| and containing |<paramref name="textLen"/>|
    /// characters.
    /// </summary>
    public unsafe IEnumerable<FilePositionSpan> SearchAll(IntPtr textPtr, int textLen) {
      List<FilePositionSpan> result = null;
      // Note: From C# spec: If E is zero, then no allocation is made, and
      // the pointer returned is implementation-defined. 
      byte* searchBuffer = stackalloc byte[this.SearchBufferSize];
      var searchParams = new NativeMethods.SearchParams {
        TextStart = textPtr,
        TextLength = textLen,
        SearchBuffer = new IntPtr(searchBuffer),
      };
      while (true) {
        Search(ref searchParams);
        if (searchParams.MatchStart == IntPtr.Zero)
          break;

        if (result == null)
          result = new List<FilePositionSpan>();
        // TODO(rpaquay): We are limited to 2GB for now.
        var offset = Pointers.Offset32(textPtr, searchParams.MatchStart);
        result.Add(new FilePositionSpan {
          Position = offset,
          Length = searchParams.MatchLength
        });
      }
      return result ?? NoResult;
    }
  }
}
