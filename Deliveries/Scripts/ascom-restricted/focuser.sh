
# vim:se ai terse nows ts=4 sts=4 sw=4 expandtab:

focuser=${ascom_server}/focuser/0

# info
function focuser_drivername() {
    GET ${focuser}/Name
}

function focuser_driverversion() {
    GET ${focuser}/DriverVersion
}

function focuser_driverinfo() {
    GET ${focuser}/DriverInfo
}

function focuser_absolute() {
    GET ${focuser}/Absolute
}

function focuser_ismoving() {
    GET ${focuser}/IsMoving
}

function focuser_maxincrement() {
    GET ${focuser}/MaxIncrement
}

function focuser_maxstep() {
    GET ${focuser}/MaxStep
}

function focuser_position() {
    GET ${focuser}/Position
}

function focuser_stepsize() {
    GET ${focuser}/StepSize
}

function focuser_move() {
    local position="${1}"

    PUT ${focuser}/Move "Position=${position}"
}

function focuser_halt() {
    PUT ${focuser}/Halt
}

function focuser_connected() {
    local arg="${1}"

    if [ "${arg}" ]; then
        PUT ${focuser}/Connected "Connected=$(to_boolean ${arg})"
    else
        GET ${focuser}/Connected
    fi
}

function focuser_status() {
    PUT ${focuser}/Action "Action=status&Parameters="
}
