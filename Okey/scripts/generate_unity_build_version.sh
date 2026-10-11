#! /usr/bin/env sh

# Script that automatically generates a Unity build version based on the current Git tag.

LAST_TAGS=$(git for-each-ref --sort='-*committerdate' --count=2 --format '%(refname:short)' refs/tags)

CURRENT_TAG=$(echo "$LAST_TAGS" | head -n 1)

CURRENT_BRANCH=$(git branch --show-current)

CURRENT_COMMIT_HASH=$(git rev-parse --short HEAD)

printf "%s+%s" $CURRENT_TAG $CURRENT_COMMIT_HASH
