local json = require 'bt.tools.json'
_class =  {}
local setmetatableindex_

setmetatableindex_ = function(t, index)
    if type(t) ~= "userdata" then
        local mt = getmetatable(t)
        if not mt then
            mt =  {}
        end
        if not mt.__index then
            mt.__index = index
            setmetatable(t, mt)
        elseif mt.__index  ~= index then
            setmetatableindex_(mt, index)
        end
    end
end

setmetatableindex = setmetatableindex_

bt =  {
    VERSION         = "0.0.1", 
    ASSERT_DIR      = "C:/work/project/LuaBT/test/",
    ASSERT_SUFFIX   = ".BT",
    deltaTime       = 1, 
    frameCount      = 0,
    time            = 0,
    Status = {
        Failure     = 0,
        Success     = 1, 
        Running     = 2, 
        Resting     = 3, 
        Error       = 4, 
        Optional    = 5
    }, 
    guards = {},
    getStatusInfo = function (status)
        if     status == bt.Status.Failure then return "Failure"
        elseif status == bt.Status.Success then return "Success"
        elseif status == bt.Status.Running then return "Running"
        elseif status == bt.Status.Resting then return "Resting"
        elseif status == bt.Status.Error   then return "Error"
        elseif status == bt.Status.Optional then return "Optional"
        else return "Unkown:"..status end
    end, 

    Class = function (classname, ...)
        local cls =  {__cname = classname}
        local supers =  {...}
        for _, super in ipairs(supers) do
            local superType = type(super)
            assert(superType == "nil"or superType == "table"or superType == "function", 
            string.format("class() - create class \" % s\" with invalid super class type \" % s\"", 
                classname, superType))
            if superType == "function" then
                assert(cls.__create == nil, 
                string.format("class() - create class \" % s\" with more than one creating function", 
                    classname)); --if super is function, set it to __create
                cls.__create = super
            elseif superType == "table" then
                if super[".isclass"] then--super is native class
                    assert(cls.__create == nil, 
                    string.format("class() - create class \" % s\" with more than one creating function or native class", 
                        classname)); 
                    cls.__create = function() return super:create() end
                else--super is pure lua class
                    cls.__supers = cls.__supers or {}
                    cls.__supers[#cls.__supers + 1] = super
                    if not cls.super then--set first super pure lua class as class.super
                        cls.super = super
                    end
                end
            else
                error(string.format("class() - create class \" % s\" with invalid super type", 
                    classname), 0)
            end
        end

        cls.__index = cls
        if not cls.__supers or #cls.__supers == 1 then
            setmetatable(cls,  {__index = cls.super})
        else
            setmetatable(cls,  {__index = function(_, key)
                local supers = cls.__supers
                for i = 1, #supers do
                    local super = supers[i]
                    if super[key] then return super[key] end
                end
            end})
        end
        if not cls.ctor then--add default constructor
            cls.ctor = function() end
        end

        cls.New = function(...)
            local instance
            if cls.__create then
                instance = cls.__create(...)
            else
                instance =  {}
            end
            setmetatableindex(instance, cls)
            instance.class = cls
            instance:ctor(...)
            return instance
        end

        cls.new = cls.New

        cls.Create = function(_, ...)
            return cls.New(...)
        end
        return cls
    end,

    decodeJson = function(str)
        return json.decode(str)
    end, 

    getCls = function(clspath, spec)
        local type
        local Cls = nil
        for t in string.gmatch(clspath, "%a+") do
            type = t
        end
        local isCondition, _ = string.find(clspath, "Conditions.")
        if type == "Timeout" and isCondition then
            type = "C".. type
        end
        if type == "LuaAction" then
            type = spec._action.luaCls._value
        end
        if type == "LuaCondition" then
            type = spec._condition.luaCls._value
        end
        if bt[type] then
            Cls = bt[type]
        else
            print("ERROR:bt.getCls:invalid class,fullpath=".. clspath .. ",subpath="..type)
        end
        return Cls
    end,

    loopFunc = {},
    loopFuncParams = {},
    loopFuncIndex = 1,

    addLoopFunc = function (func,param)
        local id = bt.loopFuncIndex
        bt.loopFunc[id] = func
        if param ~= nil then
            bt.loopFuncParams[id] = param
        end
        bt.loopFuncIndex =  bt.loopFuncIndex + 1
        return id
    end,

    delLoopFunc = function (id)
        if bt.loopFunc[id] ~= nil then
            bt.loopFunc[id] = nil
            bt.loopFuncParams[id] = nil
        end
        if bt.loopFuncParams[id] ~= nil then
            bt.loopFuncParams[id] = nil
        end
    end,

    runLoopFunc = function()
        for k,func in pairs(bt.loopFunc) do
            if bt.loopFuncParams[k] ~= nil then
                func(bt.loopFuncParams[k])
            else 
                func()
            end
        end
    end,
}
