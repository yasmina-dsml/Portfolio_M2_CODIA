#! /usr/bin/env sh

sed "s/readonly_pass/${MYSQL_READONLY_PASS}/" ../init.sql | sed "s/admin_pass/${MYSQL_ADMIN_PASS}/" | mysql -uroot -proot
