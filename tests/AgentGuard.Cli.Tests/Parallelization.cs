// Copyright (c) 4thWAIV. All rights reserved.

using Xunit;

// The CLI tests drive the real entry point in-process, mutating process-global state (the working directory, the
// HOME environment variable, and the console streams). They must run sequentially so those redirections never
// overlap.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
