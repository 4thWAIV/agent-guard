// Copyright (c) 4thWAIV. All rights reserved.

using Xunit;

// The CLI tests drive the internal Program.Run seam in-process against the real filesystem, each command creating
// and removing a real installed machine (real version-store symlinks) under its own isolated temp root. The
// environment, home, working directory, and console are all injected, so no process-global state is shared — but
// the suite is kept sequential so the concurrent real-filesystem installs stay simple and cannot contend.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
