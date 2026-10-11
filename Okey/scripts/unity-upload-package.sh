#!/usr/bin/env sh

# Script to be used exclusively in GitLab CI/CD pipelines to upload Unity packages to the GitLab Package Registry

# Iterate over all the build artifacts from previous jobs in the current directory
PACKAGE_HEADER=$(printf "%s_%s" "Unity" "$(echo $ARCHIVE_PATH | cut -d '_' -f '1-3')") # get the package name from the archive filename until the version info and add 'Unity' as a prefix
PACKAGE_PLATFORM=$(echo "$ARCHIVE_PATH" | cut -d '_' -f '2')                           # get the package name from the archive filename until the version info and add 'Unity' as a prefix
PACKAGE_VERSION=$(echo "$CI_COMMIT_TAG" | cut -b '2-')                                 # extract the version from the tag, removing the 'v' prefix
PACKAGE_FILENAME=$(echo "$ARCHIVE_PATH" | sed 's/+/_/')                                # replace the '+' with '_' in the filename for the package
UPLOAD_URL="${CI_API_V4_URL}/projects/${CI_PROJECT_ID}/packages/generic/${CLIENT_PACKAGE_NAME}/${PACKAGE_VERSION}/${PACKAGE_FILENAME}"

curl --header "JOB-TOKEN: $CI_JOB_TOKEN" --upload-file "$ARCHIVE_PATH" "$UPLOAD_URL"

echo "${PACKAGE_PLATFORM}_LINK_NAME=$PACKAGE_HEADER" >>"$ENV_FILE" # asset link name
echo "${PACKAGE_PLATFORM}_LINK_URL=$UPLOAD_URL" >>"$ENV_FILE"      # asset link URL
