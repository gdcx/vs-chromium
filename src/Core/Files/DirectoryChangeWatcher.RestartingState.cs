﻿// Copyright 2018 The Chromium Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

using System;
using System.Collections.Generic;
using System.IO;

namespace VsChromium.Core.Files {
  public partial class DirectoryChangeWatcher {
    private class RestartingState : State {
      /// <summary>
      /// The date/time when we restarted watching files, but still observing disk activity before
      /// resuming notifications.
      /// </summary>
      private readonly DateTime? _enteredStateUtc;

      public RestartingState(SharedState sharedState) : base(sharedState) {
        _enteredStateUtc = SharedState.ParentWatcher._dateTimeProvider.UtcNow;
      }

      public override void OnStateActive() {
        StartWatchers();
      }

      public override State OnResume() {
        SharedState.ParentWatcher.OnResumed();
        return new RunningState(SharedState);
      }

      public override State OnPause() {
        StopWatchers();
        return new PausedState(SharedState);
      }

      public override State OnWatchDirectories(IEnumerable<FullPath> directories) {
        WatchDirectoriesImpl(directories);
        return this;
      }

      public override State OnPolling() {
        var span = SharedState.ParentWatcher._dateTimeProvider.UtcNow - _enteredStateUtc;
        if (span > SharedState.ParentWatcher._autoRestartObservePeriod) {
          // We have not had any events so we can restart evertything
          SharedState.ParentWatcher.OnResumed();
          return new RunningState(SharedState);
        }
        return this;
      }

      public override State OnWatcherErrorEvent(object sender, ErrorEventArgs args) {
        return BackToErrorState();
      }

      public override State OnWatcherFileChangedEvent(object sender, FileSystemEventArgs args, PathKind pathKind) {
        return BackToErrorState();
      }

      public override State OnWatcherFileCreatedEvent(object sender, FileSystemEventArgs args, PathKind pathKind) {
        return BackToErrorState();
      }

      public override State OnWatcherFileDeletedEvent(object sender, FileSystemEventArgs args, PathKind pathKind) {
        return BackToErrorState();
      }

      public override State OnWatcherFileRenamedEvent(object sender, RenamedEventArgs args, PathKind pathKind) {
        return BackToErrorState();
      }

      public override State OnWatcherAdded(FullPath directory, DirectoryWatcherhEntry watcher) {
        return BackToErrorState();
      }

      public override State OnWatcherRemoved(FullPath directory, DirectoryWatcherhEntry watcher) {
        return BackToErrorState();
      }

      private State BackToErrorState() {
        StopWatchers();
        return new ErrorState(SharedState);
      }
    }
  }
}