
# vim:se ai terse nows ts=4 sts=4 sw=4 expandtab:

safetooperate=${ascom_server}/safetymonitor/0

function safetooperate_drivername() {
    GET ${safetooperate}/Name
}

function safetooperate_driverversion() {
    GET ${safetooperate}/DriverVersion
}

function safetooperate_driverinfo() {
    GET ${safetooperate}/DriverInfo
}

function safetooperate_issafe() {
    GET ${safetooperate}/issafe 
}

function safetooperate_status() {
    PUT ${safetooperate}/Action "Action=status&Parameters="
}

function safetooperate_unsafereasons() {
    PUT ${safetooperate}/Action "Action=unsafereasons&Parameters="
}

function safetooperate_supportedactions() {
    GET ${safetooperate}/SupportedActions
}

function safetooperate_bypass() {
    local arg="${1}"

    if [ "${arg}" ]; then
        PUT ${safetooperate}/Action "Action=bypass&Parameters=$(to_boolean ${arg})"
    else
        PUT ${safetooperate}/Action "Action=bypass&Parameters="
    fi
}

function safetooperate_connected() {
    local arg="${1}"

    if [ "${arg}" ]; then
        PUT ${safetooperate}/Connected "Connected=$(to_boolean ${arg})"
    else
        GET ${safetooperate}/Connected
    fi
}
