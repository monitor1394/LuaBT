

tbNameSpace = tbNameSpace or {}
tbRequireName = tbRequireName or {}

function namespace(strSpaceName)
    if not tbNameSpace[strSpaceName] then
        local M = {}
        _G[strSpaceName] = M
        setmetatable(M, {__index = _G })
        _ENV[strSpaceName] = M
        tbNameSpace[strSpaceName] = true
    else
        print("ERRPR:namespace have existed : " .. strSpaceName)
        _ENV[strSpaceName] = _G[strSpaceName]
    end
end

local oldRequire = require
require = function (str)
    if not tbRequireName[str] then
        local ret = oldRequire(str)
        tbRequireName[str] = true
        return ret
    else
        print("ERROR:require is repeated : " .. str)
        return nil
    end
end

function clearAllNamespace()
    for k,v in pairs(tbNameSpace) do
        _G[k] = nil
    end
    for k,v in pairs(tbRequireName) do
        package.loaded[k] = nil
    end
    tbNameSpace = {}
    tbRequireName = {}
end