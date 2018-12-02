#!/bin/bash

# vim:se ai terse nows ts=4 sts=4 sw=4 expandtab:

export PS1='\[\e[33m\]Wise40 \[\e[0m\]>>> '
export PATH=bin

ascom_server_ip=132.66.65.9
ascom_server_port=11111
ascom_server=http://${ascom_server_ip}:${ascom_server_port}/api/v1

declare -A ansi_colors=(
    [black]="$(tput setaf 0)"
    [red]="$(tput setaf 1)"
    [green]="$(tput setaf 2)"
    [yellow]="$(tput setaf 3)"
    [blue]="$(tput setaf 4)"
    [magenta]="$(tput setaf 5)"
    [cyan]="$(tput setaf 6)"
    [white]="$(tput setaf 7)"
)

function colored_text() {
    local color="${1}"
    shift 1
    local text="${*}"
    
    if [[ $- == *i* ]]; then
        echo -e -n "${ansi_colors[${color}]}${text}${ansi_colors[white]}"
    else
        echo -e -n ${text}
    fi
}

function error() {
    colored_text red "ERROR: ${*}\n" >&2
}

function warning() {
    colored_text yellow "WARNING: ${*}\n" >&2
}

function good() {
    colored_text green "${*}"
}

function to_boolean() {
    case "${1,,}" in
        on|yes|1|true)
            echo true
            ;;
        off|no|0|false)
            echo false
            ;;
        *)
            error "${FUNCNAME}: cannot convert \"${1}\" to a boolean!"
            ;;
    esac
}

function parse_response() {
    local response="" line

    response="$( cat - )"

    if [[ "${response}" == [45]00* ]]; then
        read error_code error_message <<< ${response}
        error "Server: ${error_message}"
        return
    fi

    local driver_exception error_number error_message value

    driver_exception="$( echo "${response}" | jq -r .DriverException )"
    if [ "${driver_exception}" ] &&  [ "${driver_exception}" != null ]; then
        local message="$( echo "${driver_exception}" | jq -r .Message)"
        local className="$( echo "${driver_exception}" | jq -r .ClassName)"
        error "Exception: ${className}: ${message}"
        return 1
    fi

    error_number="$(echo "${response}" | jq -r .ErrorNumber )"
    error_message="$(echo "${response}" | jq -r .ErrorMessage )"
    if [ ${error_number} != 0 ] || [ "${error_message}" ]; then
        error "Error: number: ${error_number}, message: ${error_message}"
        return 1
    fi

    local value="$(echo "${response}" | jq -r .Value )"
    if [ "${value}" ] && [ "${value}" != null ]; then
        echo "${value}"
    fi

    return 0
}

function ascom_server_is_alive() {
    local dummy=$(curl --fail --connect-timeout 2 --silent -X GET http://${ascom_server_ip}:${ascom_server_port}/server/v1/concurrency)
    local status=$?
    if [ ${status} != 0 ]; then
        error "server http://${ascom_server_ip}:${ascom_server_port}/server/v1/concurrency not responding (status: ${status})"
        return ${status}
    fi
}

function PUT() {
    local url="${1}"
    shift
    local data="${@}"
    local response status

    ascom_server_is_alive || return $?

    response="$( curl --connect-timeout 2 --silent -X PUT --header 'Content-Type: application/x-www-form-urlencoded' --header 'Accept: application/json' --data "${data}" ${url} )"
    status=$?
    if [ ${status} -eq 0 ]; then
        echo "${response}" | parse_response
    fi
    return ${status}
}

function GET() {
    local url="${1}"
    local response status

    ascom_server_is_alive || return $?

    response="$( curl --connect-timeout 2 --silent -X GET ${url} )"
    status=$?
    if [ ${status} -eq 0 ]; then
        echo "${response}" | parse_response
    fi
    return ${status}
}

for file in telescope dome computer-control safetooperate site shutter focuser weather; do
    source ${file}.sh
done
