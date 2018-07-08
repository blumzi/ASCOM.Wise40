#!/usr/bin/gawk -f

# 12:16:08.099 => millis since epoch
function get_millis(hms, m) {
    m = hms
    sub(".*[.]", "", m)
    sub("[.].*", "", hms)
    gsub(":", " ", hms)
    return mktime("2018 1 1 "hms) * 1000 + m
}

BEGIN {
    print "{"
}

END {
    print "}"
}

{
    parse($0)
}

function indent(level, s, i, out) {
    for (i = 0; i < level; i++ )
        out = out "  "
    return out s
}

function parse(line, verb, unit, n, tid, url, millis, method, params, driver, pars, p, out, t, words, exception, active, json, v, value, msg) {
    millis = get_millis($1)
    tid = $4

    if ($6 == "GET" || $6 == "PUT") {
        verb = $6
        url = $0
        sub(".*v1/", "", url)
        sub(",.*", "", url)

        split(url, words, "/")
        driver = words[1]
        unit = words[2]
        method = words[3]
        sub("?.*", "", method)
        params = words[3]
        sub(".*[?]", "", params)
        split(params, pars, "&")

        transaction[tid, "start"] = millis
        transaction[tid, "verb"] = verb
        transaction[tid, "url"] = url
        transaction[tid, "driver"] = driver
        transaction[tid, "unit"] = unit
        transaction[tid, "method"] = method
        transaction[tid, "params"] = params
        for (t in active)
            if (transaction[tid, "active"])
                transaction[tid, "active"] = transaction[tid, "active"] " " t
            else
                transaction[tid, "active"] = t
        active[tid] = 1
        #print "transaction[" tid ", driver] = " transaction[tid, "driver"] 
        #print "transaction[" tid ", verb] = " transaction[tid, "verb"] 
    } else if ($6 == "OK") {
        transaction[tid, "end"] = millis
        if (transaction[tid, "start"])
            transaction[tid, "duration"] = millis - transaction[tid, "start"]
        json = line
        sub(".*Json: ", "", json)
        #print "json: " json
        # Escape commas within Json values, otherwise they will split the lines
        if (match(json, "Value\":\"[^\"]*\"", v)) {
            rstart = RSTART
            rlength = RLENGTH
            value = v[0]
            #print "value: ===" value "==="
            gsub(",", "@@@", value)
            #print "value: ---" value "---"
            json = substr(json, 1, rstart - 1) value substr(json, rstart + rlength)
            #print "json: " json
        }
        #gsub("\x00\xB0", "Celsius", json)
        gsub(",", ",\n      ", json)
        sub("{", "{\n      ", json)
        sub("}", "\n    }", json)
        gsub("\r", "", json)
        gsub(":", ": ", json)
        gsub("@@@", ",", json)
        transaction[tid, "json"] = json
        produce_transaction_entry(tid, 0)
    } else if ($6 == "Exception:") {
        transaction[tid, "end"] = millis
        if (transaction[tid, "start"])
            transaction[tid, "duration"] = millis - transaction[tid, "start"]
        exception = line
        sub(".*Exception: ", "", exception)
        sub("\r", "", exception)
        msg = substr(line, index(line, "Exception") + length("Exception: "))
        sub("\r", "", msg)
        transaction[tid, "json"] = "{\n  " indent(2, quoted("Exception") ": " quoted(msg)) "\n" indent(2, "}")
        produce_transaction_entry(tid, 0)
    } else if ($6 == "Parameter") {
        #print line
        if (! transaction[tid, "nparams"])
            transaction[tid, "nparams"] = 0
        sub("\r", "", $7)
        sub("\r", "", $9)
        n = transaction[tid, "nparams"]
        transaction[tid, "param" n] = $7"="$9
        transaction[tid, "nparams"] = n + 1
    }
}

function field(label, content, numeric) {
    return indent(2, sprintf("%s: %s,\n", quoted(label), 
           numeric ? content : quoted(content)))
}

function result(label, content, numeric) {
    return indent(2, sprintf("%s: %s\n", quoted(label), 
           numeric ? content : quoted(content)))
}

function param(p, last, word, out, name, value) {
    split(p, word, "=")
    name = word[1]
    value = word[2]
    out = indent(3, quoted("param") ": {\n")
    out = out indent(4, sprintf("%s: %s,\n", quoted("name"), quoted(name)))
    out = out indent(4, sprintf("%s: %s\n", quoted("value"), quoted(value)))
    out = out indent(3, sprintf("}%s\n", last ? "" : ","))
    return out
}

function quoted(s) {
    return "\""s"\""
}

function produce_transaction_entry(tid, last, _out, _t) {
        _out = _out indent(1, quoted("server-transaction" "-" tid) ": {\n")
        _out = _out field("id",           tid, 1)
        _out = _out field("verb",         transaction[tid, "verb"], 0)
        _out = _out field("driver",       transaction[tid, "driver"], 0)
        _out = _out field("unit",         transaction[tid, "unit"], 1)
        _out = _out field("method",       transaction[tid, "method"], 0)
        if (transaction[tid, "nparams"]) {
            _out = _out indent(2, quoted("params") ": {\n")
            for (n = 0; n < transaction[tid, "nparams"]; n++)
                _out = _out param(transaction[tid, "param" n], 
                    (n+1) == transaction[tid, "nparams"] ? 1 : 0)
            _out = _out indent(2, "},\n")
        }
        _out = _out field("start-millis", transaction[tid, "start"], 1)

        if (transaction[tid, "end"] != "") {
            _out = _out field("end-millis",   transaction[tid, "end"], 1)
            _out = _out field("duration-millis", transaction[tid, "duration"], 1)
        }
        _out = _out field("exception",    transaction[tid, "exception"], 0)

        sub(" $", "", _active)

        _out = _out result("result",      transaction[tid, "json"], 1)
        _out = _out indent(1, sprintf("}%s", last ? "" : ",\n"))

        delete transaction[tid]
        print _out
}
