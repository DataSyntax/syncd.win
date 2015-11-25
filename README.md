# syncd.win
Syncd daemon for boot2docker (WINDOWS version).

SyncD synchronizes all changes you do in your project with Docker host (boot2docker) machine *in real time*
This means it listens to all file change events inside your project folder, and fires rsync. If during
running rsync session new file changes happen, new rsync session is started after the first one is completed.
This makes a reliable and fast solution (which tops the `vagrant rsync-auto` in many ways), and makes your work
efficient with docker even on Windows and OSX.

Since all changes are synchronized to docker-host machine, there is no slowdown connected with using network
filesystems like SMB or NFS.

Syncd has a flexible configuration file (syncd.conf) where you can configure what to rsync and what not,
and intuitive interface.

`syncd start`  - start syncd daemon for current folder
`syncd stop`   - stop syncd daemon
`syncd status` - tell if syncd is already monitoring the folder, and if yes PID of the process
`syncd run`    - one time run of the synchronization command rsync

Configuration file (syncd.conf) must exist inside folder in all cases.
Format of the file is self-explanatory.

