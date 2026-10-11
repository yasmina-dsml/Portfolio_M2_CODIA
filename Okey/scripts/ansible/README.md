# Ansible playbook for setting up a new server

Here are a few details on how to use this Ansible configuration.

## Prerequisites and assumptions for the target systems

* CPU architecture is assumed to be **amd64**
* Username assumed to be **ubuntu**
* OS is assumed to be Ubuntu and `apt` package manager available
* Python is assumed to be available

## Prerequisites and assumptions for the host system

* SSH access for the inventory VMs are configured properly
* Ansible is installed
* You're connected to Unistra's network either directly or via a VPN (see <https://vpn.unistra.fr>)

## Running the playbook

You can run the playbook with the following command:

```bash
ansible-playbook -i inventory.yml playbook.yml
```

You can also use the convenience script `ansible.sh` instead to run the playbook.
