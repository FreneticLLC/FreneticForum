# FreneticForum

Welcome to the Frenetic Forum, built in ASP.NET Core MVC!

If you're seeing this, you're probably horrendously lost.

### Notice

At current stage, FreneticForum is Copyright (C) 2016-2021 FreneticLLC, All Rights Reserved.

Licensing is likely to change in the future.

### Project Status

Early development. Nothing is remotely ready to use!

### Setup

- Create, setup, and reasonably secure a MongoDB server instance.
    - You will need a (non-admin) user with `readWrite` access to a single db.
- Install .NET Core CLI.
- Install NPM (Node Package Manager, generally included with package labeled like `nodejs`, may require special install steps - google helps!).
- Install NPM package `gulp` and related (at a command line, `npm install gulp rimraf gulp-concat gulp-cssmin gulp-uglify gulp-rename`).
    - Probably need to `npm install -g gulp` (with the `-g` flag) as root.
- Probably blacklist the launch port (which is `8050` by default) to be disallowed for everyone but you - for safety during the setup phase.
- Download this source repository, open a command line to its directory, restore associated data (run `dotnet restore`) and run it (run `development.bat` or `start.bat` / `start.sh`).
- Connect to the server in the path its at (Server URL (or `localhost`), with port `8050` by default) to be automatically directed to the install page.
    - Make sure config file folder is editable by the server process user. (Defaults to local path, `./config/`)
    - To change your config file folder location, edit the marked variable near the top of `ForumInit.cs`.
- Configure everything as per instructions, and press the button at the bottom.
- You can log in as `admin` with the password you gave on the install page.
- It is recommended at this point that you register yourself a separate account, and give it admin access of its own, so that you are not logging in as the root admin normally.
- Configure the forum however you wish via the administrative control panel.
- If you blacklisted the launch port, at this point remove that blacklisting.
- Optionally, use apache or similar as a mid-point for connections.
- Invite some users and start posting!

### Plan / Outline

- General
    - Everything backed by MongoDB for data.
- Account System
    - A simple Collection of users, stored as documents.
    - A full account management setup with registration, login, logout, etc.
    - Usable as a generic account-server.
    - Semi-relational data, not all dumped into the user document.
- BBCode Parser
    - An engine to parse BBCode.
    - Supports an administrator-definable list of BBCodes, including a sample set that can be modified.
- Forum
    - Index
        - Display a list of all sections.
            - Sorted by administrators manually.
            - Info on each section, including latest post meta.
            - No pagination.
            - Broken up into Categories (not separately viewable).
    - Section Index
        - Display a list of all topics in section.
            - Sorted by changable criteria, with default mode specified globally or per-section by administrators.
            - Info on each topic, including latest message meta and original message meta.
            - Paginated.
    - Topic View
        - Display a list of all posts in topic.
            - Sorted by date, always.
            - Paginated.
            - Full contents of each post. Meta on left side, content on right side (main space).
                - Contents are BBCode-Parsed.
- Related
    - Installer System
        - Only available once per install of a forum.
        - Shows up whenever configuration file is not-found.
        - Creates the configuration file. (Admins are informed to allow access to the specific config file, or change the file an easily edited source text file!)
        - Initializes the backing database with some basic empty collections, and one default admin user, which is configured by installing admin.
    - Admin Panel
        - Full control of the entire system.
    - Moderator Panel
        - Control over and information on user activities (EG reports).
    - User Control Panel
        - Good level of control for a user over their own account, alongside basic settings such as board theme.
    - User Private Messaging (PM) Service
        - Way for users to contact each other.
- Other
    - Simple 'pages builder'.
        - A way to build generic web pages easily within the forum's domain.
- Search Engine
    - Maintain a mapping of keywords to posts
        - IE, for each word in the post that should be validly searchable, set an indexed database key with that name to contain a reference ID of the post.

### Database Outline

- tf_users
    - Index on: `uid` (long), `username` (string)
    - Also has: `email` (string), `display_name` (string), `password` (hashed binary string)
    - Also has: `banned` (bool), `banned_until` (date string), `ban_reason` (string)
    - Also has: `active` (bool), `activation_code` (string)
    - Also has: `register_date` (date string), `last_login_date` (date string)
    - Also has: `uses_tfa` (bool), `tfa_internal` (string), `tfa_backups` (string)
    - Also has: `account_type` (int32), `roles` (array of strings)
    - Also has: `websess_codes` (array of strings)
- tf_settings
    - Index on: `name` (string)
    - Also has: `value` (string)
- tf_sections
    - Index on: `name` (string), `uid` (long)
    - Also has: `description` (string)
- tf_topics
    - Index on: `uid` (long) `section_id` (long)
    - Also has: `title` (string), `main_post` (long), `post_uids` (array of longs)
    - Also has `tags` (array of strings)
- tf_posts
    - Index on: `uid` (long)
    - Also has: `contents` (BBCode string), `author_uid` (long), `author_username` (string), `post_date` (date string)
- tf_search
    - Index on: `keyword` (string)
    - Also has: `posts` (array of int32s)

### Random Concept Write-Ups

- Forum user/post reporting
    - Rather than provide a built-in report system separate from existing functionality,
    - It is likely best to set up a "quiet" "self-only" forum section.
    - This section would show a user their own reports and postings in it by themselves and admins, but not other users' reports.
    - Admins can freely browse the section.
    - Goes well with the tag system to mark a report open/resolved/etc.
- Tag System
    - Forum topics can be tagged.
    - This is for searchability and content visibility.
    - EG, a topic might be tagged "Open" initially, then changed to "Resolved" later.
    - Allows multiple tags per topic.
    - Common tags can be configured by admins to be selected-from and given special indication color.
    - Alternately, users can specify their own uncolored tags.
