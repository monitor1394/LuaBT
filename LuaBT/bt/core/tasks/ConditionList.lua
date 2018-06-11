
-----------------------------------------------------------------------------------------
-- ConditionList is a ConditionTask itself that holds many ConditionTasks. It can be set 
-- to either require all true or any true.
-----------------------------------------------------------------------------------------
local ConditionList = bt.Class("ConditionList",bt.ConditionTask)
bt.ConditionList = ConditionList

ConditionsCheckMode = {
    AllTrueRequired = 0,
    AnyTrueSuffice  = 1,
}

function ConditionList:ctor()
    bt.ConditionTask.ctor(self)
    self.name = "ConditionList"
    self.conditions = {}
    self.checkMode = ConditionsCheckMode.AllTrueRequired
    self.initialActiveConditions = nil
end

function ConditionList:init(jsonData)
    if jsonData.checkMode ~= nil then
        self.checkMode = ConditionsCheckMode[jsonData.checkMode]
    end
    local size = #jsonData.conditions
    for i=1,size do
        local jsonData2 = jsonData.conditions[i]
        Cls = bt.getCls(jsonData2["$type"], jsonData2)
        local condition = Cls.new()
        condition:init(jsonData2)
        table.insert(self.conditions, condition)
    end
end

function ConditionList:isAllTrueRequired()
    return self.checkMode == ConditionsCheckMode.AllTrueRequired
end

function ConditionList:info()
    if #self.conditions == 0 then
        return "No Conditions"
    end
    local finalText = ""
    if self.isAllTrueRequired() then
        finalText = "ALL True\n"
    else
        finalText = "ANY True\n"
    end
    for k,condition in pairs(self.conditions) do
        while true do
            if condition == nil then
                break
            end
            if condition.isActive or self:isContainInInitalActiveConditions(condition) then
                finalText = finalText .. condition.info() .. "\n"
            end
            break
        end
    end
    return finalText
end

function ConditionList:isContainInInitalActiveConditions(condition)
    if self.initialActiveConditions == nil then
        return false
    end
    for k,v in pairs(self.initialActiveConditions) do
        if v == condition then return true end
    end
    return false
end

function ConditionList:onEnable()
    if self.initialActiveConditions == nil then
        self.initialActiveConditions = {}
        for k,v in pairs(self.conditions) do
            if v.isActive then
                table.insert(self.initialActiveConditions, v)
            end
        end
    end
    for k,v in pairs(self.initialActiveConditions) do
        v:enable(agent,blackboard)
    end
end

function ConditionList:onDisable()
    for k,v in pairs(self.initialActiveConditions) do
        v:onDisable()
    end
end

function ConditionList:onCheck()
    local succeedChecks = 0
    for i=1,#self.conditions do
        while true do
            if not self.conditions[i].isActive then
                succeedChecks = succeedChecks + 1
                break
            end
            if self.conditions[i]:checkCondition(agent,blackboard) then
                if not self:isAllTrueRequired() then
                    return true
                end
                succeedChecks = succeedChecks + 1
            else
                if self:isAllTrueRequired() then
                    return false
                end
            end
            break
        end
    end
    return succeedChecks == #self.conditions
end

function ConditionList:destroy()
    for k,condition in pairs(self.initialActiveConditions) do
        if condition ~= nil then
            condition:destroy()
            condition = nil
        end
    end
    self.initialActiveConditions = nil

    for k,condition in pairs(self.conditions) do
        if condition ~= nil then
            condition:destroy()
            condition = nil
        end
    end
    self.conditions = nil
end
