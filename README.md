This is just a little standalone Kanban board.   

It supports multiple projects, dynamically addable and removable columns (or 
statuses or whatever your kanban flavour calls them).

It has limited support for tagging with colours to flag important tasks, estimated
times, highlighting when tasks are becoming overdue, and quick drag/drop of tasks
between the columns.  There's even light/dark mode.

It's written with Avalonia to make it portable, so it runs okay on Linux under dotnet.

The tasks are all stored in a tiny local SQLite instance, and the tasks can be exported
to CSV if you want to move them somewhere else.

I wrote it for myself because my employer locks down our machines and forces us to 
use Teams. I'd rather not use Teams if I can help it.

Chunks of this (especially the more complex Avalonia wrangling) were written by Claude
because who has time to write all that godforsaken AXAML.  I just wish there was a 
proper visual editor.  Avalonia's a good project, but some stuff is just too mind-
numbingly hard for what you need to achieve, especially when you use Fluent Theme
and need to do something unusual.

This is made public with no apologies, and no guarantees.  If you like it, enjoy it.
:)
