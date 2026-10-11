#! /usr/bin/env sh

##### This is a convenience script to run the ansible playbook #####
# If you want you can directly run the ansible-playbook command instead

# Get the directory where the script is located
DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd -P)

# Change the directory to the script's location
cd "$DIR" || exit

# Run the ansible playbook
ansible-playbook -i inventory.yml playbook.yml
