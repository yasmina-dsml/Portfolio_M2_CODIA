"""
Script to cross reference the gitlab issue number with the commit message

This script will be called by `gitlint` as a custom user rule during the
`commit-msg` git hook stage. Before this script is called, another script
(`giticket`) during the `prepare-commit-msg` stage will be called to
generate the commit message and prepend the gitlab issue number to the
beginning of the commit message (in the title) so long as the branch name
contains an issue number.

This script will parse the commit message and extract the issue number in
the beginning of the message if it is present. It will then contact the
GitLab API and fetch the `iid`s of all the issues in the project.
It should check if the reference in the commit message actually is present
in the list fetched from GitLab.

Thus, arises multiple cases to consider:

* If no issue ref is present in the commit message, two cases arise:
  * If the (current) branch name does not contain an issue number, don't raise an error
    but just display a warning saying the developer is working on a branch that
    should stay local and should not be pushed to the remote since there are
    no issue refs attached to it.
  * If the (current) branch name contains an issue number, raise an error saying the
    commit message is badly formatted and is missing the issue ref.
* If the issue ref is present in the commit message, two cases arise:
  * If the ref found is also in the GitLab issue list, everything is fine
  * If the ref found is not in the GitLab issue list, raise an error

While doing all this any errors should be properly handled and logged.
Network failure should also be taken into account and API requests over
the network should have a timeout and a proper error message should be
displayed saying the full checks could not be performed due to network
error and the developer has the responsibility to check the issue ref really exists.

One should know that local git hooks can be bypassed by the developer so this script
is just an effort to help the developer and not to enforce strict rules. For those
that want to play by the rules.
"""

import os
import re
import sys
import gitlab
import requests
from dotenv import load_dotenv
from gitlint.rules import CommitRule, RuleViolation

class IssueRefExists(CommitRule):
    """
    Help and inform the devs in attaching a valid GitLab issue ref to
    their commit messages.
    """

    # A rule MUST have a human friendly name
    name = "issue-ref-exists"

    # A rule MUST have a *unique* id
    # We recommend starting with UC (for User-defined Commit-rule).
    id = "UC1"

    def validate(self, commit):
        """
        Couple tips:

        * The `re.match()` or `re.search()` methods can be used to match the issue number or branch name
        * The regex used can be something like `r"^GL-(\d+): "` and the capture group 1 can be used to extract the issue number only
        * Some similar regex for the branch name but the branch name format is the following: `881-my_branch` for example (without `GL` in the beginning)

        See these documentation pages for more info:
        https://jorisroovers.com/gitlint/latest/rules/user_defined_rules/
        https://jorisroovers.com/gitlint/latest/rules/user_defined_rules/line_and_commit_rules/#commit-object
        https://jorisroovers.com/gitlint/latest/rules/user_defined_rules/violations/
        https://git-scm.com/book/en/v2/Customizing-Git-Git-Hooks
        https://git-scm.com/docs/githooks
        https://docs.python.org/3.10/howto/regex.html
        https://docs.python.org/3.10/library/re.html


        Extract issue reference from the commit message title:

        * Attempt to extract the issue reference from the commit message title.
        * If it's not found, you check if the branch name contains an issue number.
        * If an issue reference is found, you verify if it exists in the list of issue IDs fetched from GitLab.
        """

        def handleNetworkError():
            print("WARNING: A network error occured, can't cross reference GitLab issue number, you are responsible for making sure such an issue actually exists.", file=sys.stderr)
            return

        match = re.match(r"^GL-(\d+): ", commit.message.title)
        if not match:
            # No issue reference found in the commit message
            branch_has_issue_number = re.match(r"^\d+-" , commit.context.current_branch)
            if branch_has_issue_number:
                # Branch name contains an issue number, but commit message does not reference it
                msg = "Commit message is badly formatted and is missing the issue reference"
                return [RuleViolation(self.id, msg, line_nr=1)]
            else:
                # No issue reference in commit message and branch name
                print("WARNING: You are working on a branch that should stay local and should not be pushed to the remote since there are no issue references attached to it", file=sys.stderr)
                return


        # Load the environment variables from the `.env` file
        load_dotenv()

        # Load the gitlab token and project id from the environment
        project_token = os.getenv("GITLAB_API_KEY")
        project_id = os.getenv("GITLAB_PROJECT_ID")

        # Check for errors in loading the gitlab token and project id
        if not project_token or not project_id:
            raise ValueError("Couldn't load the gitlab token or project id from the environment. Do you have the Python dependencies installed or the `.env` file present?")


        # Create a gitlab object
        gl = gitlab.Gitlab(url="https://git.unistra.fr", private_token=project_token, timeout=10)
        # gl.enable_debug()

        # Fetch the project and its issue iids from the gitlab API
        try:
            project_okey = gl.projects.get(project_id, lazy=True)

            # Fetch the project issues from the gitlab API and filter the issue iids
            issue_iids = [i.iid for i in project_okey.issues.list(get_all=True)]
        except gitlab.exceptions.GitlabAuthenticationError as e:
            # Handle the authentication error
            print(f"GitLab authentication error: {e}", file=sys.stderr)
            handleNetworkError()
            return
        except gitlab.exceptions.GitlabGetError as e:
            # Handle the GET request error
            print(f"GitLab API error: {e}", file=sys.stderr)
            handleNetworkError()
            return
        except requests.exceptions.Timeout as e: # pas sûr si c'est la bonne exception
            # Handle the network error
            print(f"Network error: {e}", file=sys.stderr)
            handleNetworkError()
            return
        except Exception as e:
            print(f"An unexpected error occurred: {e}", file=sys.stderr)
            handleNetworkError()
            return


        # Issue reference found in the commit message
        issue_ref = int(match[1])
        if issue_ref not in issue_iids:
            # Issue reference not found in the GitLab issues list
            msg = f"Issue reference {issue_ref} in the commit message does not exist in the GitLab issues list"
            return [RuleViolation(self.id, msg, line_nr=1, content=match[0])]

        # Issue reference found in the GitLab issues list, everything seems fine
        return
