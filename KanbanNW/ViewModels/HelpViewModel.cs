using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace KanbanNW.ViewModels;

public partial class HelpViewModel : ViewModelBase
{
    public ObservableCollection<HelpTopic> Topics { get; } = new();

    [ObservableProperty]
    private HelpTopic? _selectedTopic;

    public HelpViewModel()
    {
        Topics.Add(new HelpTopic
        {
            Title = "Welcome",
            Content = "Welcome to Kanban-NW!\n\n"
                + "Kanban-NW is a lightweight, local kanban board application built with Avalonia UI. "
                + "It helps you organise tasks into columns, track progress, and stay on top of your work — "
                + "all stored locally on your machine in a SQLite database.\n\n"
                + "Use the topics on the left to learn more about each feature."
        });

        Topics.Add(new HelpTopic
        {
            Title = "What is a Kanban Board?",
            Content = "A kanban board is a visual tool for managing work. "
                + "Originating from Toyota's manufacturing process, it represents work items as cards "
                + "that move through columns representing stages of completion.\n\n"
                + "The classic columns are:\n"
                + "  • In Tray — tasks waiting to be started\n"
                + "  • (Your columns) — stages you define for your workflow\n"
                + "  • Done — completed tasks\n\n"
                + "You can add as many columns as you need between In Tray and Done to match your workflow."
        });

        Topics.Add(new HelpTopic
        {
            Title = "Getting Started",
            Content = "When you first launch Kanban-NW, a default project is created with two system columns:\n"
                + "  • In Tray\n"
                + "  • Done\n\n"
                + "To get started:\n"
                + "  1. Click '+ Add Task' in the In Tray column to create your first task.\n"
                + "  2. Click on the task to open the editor, where you can set a title, description, type, due date, and more.\n"
                + "  3. Drag the task to another column when you're ready to move it.\n\n"
                + "Use Config → Columns to add custom columns that match your workflow."
        });

        Topics.Add(new HelpTopic
        {
            Title = "Managing Tasks",
            Content = "Creating tasks:\n"
                + "  • Click '+ Add Task' at the top of any non-system column (or In Tray).\n"
                + "  • A new task appears with a default name.\n"
                + "  • Click on it to open the task editor and fill in the details.\n\n"
                + "Editing tasks:\n"
                + "  • Click on a task card to open the editor.\n"
                + "  • Change the title, description, type, due date, and estimated days.\n"
                + "  • Add comments to keep notes on a task.\n\n"
                + "Moving tasks:\n"
                + "  • Drag a task card and drop it onto another column.\n"
                + "  • A placeholder shows where the task will land.\n"
                + "  • Tasks can be reordered within the same column by dragging up and down.\n\n"
                + "Completing tasks:\n"
                + "  • Click the ✓ button on a task to mark it complete and move it to Done.\n\n"
                + "Deleting tasks:\n"
                + "  • Click the ✕ button on a task to move it to the Deleted Tasks queue.\n"
                + "  • Use 'Show Deleted' in the Config menu to view and permanently delete them."
        });

        Topics.Add(new HelpTopic
        {
            Title = "Managing Columns",
            Content = "Use Config → Columns to open the column editor.\n\n"
                + "Adding columns:\n"
                + "  • Click '+ Add Column' to add a new column to your board.\n"
                + "  • New columns appear before the Done column.\n\n"
                + "Renaming columns:\n"
                + "  • Click on a column name in the editor to change it.\n"
                + "  • Click Save to apply your changes.\n\n"
                + "Deleting columns:\n"
                + "  • Click the ✕ button next to a user-created column.\n"
                + "  • You will be asked to confirm — tasks in that column are moved to In Tray.\n"
                + "  • System columns (In Tray, Done) cannot be deleted.\n\n"
                + "Click Save to commit your changes, or Cancel to discard them."
        });

        Topics.Add(new HelpTopic
        {
            Title = "Managing Projects",
            Content = "Kanban-NW supports multiple projects. Each project has its own set of columns and tasks.\n\n"
                + "Use Config → Projects to open the project editor.\n\n"
                + "Switching projects:\n"
                + "  • Click on a project tab at the top of the window.\n\n"
                + "Adding projects:\n"
                + "  • Click '+ Add Project' in the project editor.\n"
                + "  • Choose a colour to differentiate the project tab.\n\n"
                + "Renaming projects:\n"
                + "  • Click on the project name in the editor to rename it.\n\n"
                + "Deleting projects:\n"
                + "  • Click the ✕ button on a project to delete it.\n"
                + "  • You cannot delete the last remaining project."
        });

        Topics.Add(new HelpTopic
        {
            Title = "Categories",
            Content = "Categories (task types) let you classify tasks with a colour-coded badge.\n\n"
                + "The built-in categories are:\n"
                + "  None, Red, Orange, Yellow, Green, Blue, Purple, Black\n\n"
                + "Use Config → Categories to rename them to match your workflow — "
                + "for example, 'Bug', 'Feature', 'Urgent', 'Chore'.\n\n"
                + "Click 'Reset to Defaults' to restore the original names."
        });

        Topics.Add(new HelpTopic
        {
            Title = "Appearance",
            Content = "Use Config → Appearance to customise how Kanban-NW looks.\n\n"
                + "Theme:\n"
                + "  • Light — light background with dark text (default)\n"
                + "  • Dark — dark background with light text\n"
                + "  • System — follows your operating system's theme setting\n\n"
                + "Font:\n"
                + "  • Choose from Small, Medium, or Large presets.\n"
                + "  • Or fine-tune individual font sizes for tabs, task titles, descriptions, and more.\n"
                + "  • Select a font family from the list of system fonts.\n\n"
                + "Click Save to apply, or Cancel to discard changes."
        });

        Topics.Add(new HelpTopic
        {
            Title = "Export",
            Content = "Use Kanban → Export Project to save all tasks in the current project as a CSV file.\n\n"
                + "The CSV includes:\n"
                + "  • Task ID, title, creation date, due date\n"
                + "  • Description, type, column name\n"
                + "  • Completion status, estimated days, and comments\n\n"
                + "Tasks are exported column by column, in their vertical order within each column."
        });

        Topics.Add(new HelpTopic
        {
            Title = "Enjoy!",
            Content = "Well hello!\n\n"
                + "So - Why a Kanban App? This whole thing started because I was fed up with Teams. "
                + "Despite having more money than most countries Microsoft somehow managed to bork the task UI "
                + "into a single-pane nightmare that makes using it a pain. "
                + "Sigh.  Bloody Microsoft.\n\n"
                + "Worse still, all the nice freeware and web-based kanban boards out there are"
                + "Blocked by my employer. The corporate firewall blocks productivity every ddamn time. "
                + "So I thought — fine, I'll make my own. 😤\n\n"
                + "It was also a chance to learn Avalonia properly. I'd poked at it before but never "
                + "built anything real. Turns out it's not awful.  I think designing user interfaces with axaml "
                + "is a crime against humanity, really.  How to kill the creative process? Force the designer to sit "
                + "for hours debugging stupid XML.  There has to be a better way, surely!  And the theming in "
                + "Avalonia breaks my noodle.  But... It works — and because it targets .NET, "
                + "Kanban-NW should run on Windows, macOS, or Linux with a runtime installed."
                + "No cloud. No sign-in. No tracking. Just your tasks, on your machine. How it should be.\n\n"
                + "The whole thing was built with JetBrains Rider on Kubuntu Linux, targeting .NET 10.\n\n"
                + "Now, the elephant in the room: yes, a lot of the UI code was generated by Claude Code. "
                + "The axaml wrangling finally got to me.  I was hours in to trying to create themes, poring over "
                + "documentation, looking at forum articles because somethign wasn't working, and somethign inside "
                + "me just snapped. I had had enough, so I took the King's Shilling and installe Claude Code.\n"
                + "I routed it through OpenRouter to keep the costs sensible. "
                + "Was it magic? No. Was it useful for wiring up plumbing, converting data templates, "
                + "and generally doing the sort of donkeywork that makes you question your life choices "
                + "at 2am? Absolutely. 🙂\n\n"
                + "It worked surprisingly well with my style — I tend to build things one feature at a time, "
                + "iterate as I go, and change my mind a lot. "
                + "As long as you keep a tight grip on what you actually want (rather than letting it "
                + "enthusiastically over-engineer everything), it can be a genuinely useful tool. "
                + "Think of it as a very capable sub-programmer. I trust it, but only so far....\n\n"
                + "As for the program itself — it's done. Effectively complete to what I wanted to achieve. "
                + "The kanban board is working, the columns all act like other Kanban tools, and it's powerful "
                + "enough to do what I need. "
                + "I don't have grand plans to bolt on fifty more features.\n\n"
                + "But if you find a bug, want a feature, or just want to say hi — "
                + "raise an issue on GitHub and I'll take a look if I get a chance. No promises, "
                + "but I'm not going anywhere. 🙃\n\n"
                + "Thanks for giving Kanban-NW a spin. I hope it makes your day a little more organised."
        });

        SelectedTopic = Topics[0];
    }
}

public partial class HelpTopic : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;
}