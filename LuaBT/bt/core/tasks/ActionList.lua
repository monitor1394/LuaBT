
local ActionList = bt.Class("ActionList",bt.ActionTask)
bt.ActionList = ActionList

ActionsExecutionMode = {
    ActionsRunInSequence = 0,
    ActionsRunInParallel = 1,
}

function ActionList:ctor()
    bt.ActionTask.ctor(self)
    self.name = "ActionList"
    self.actions = {}
    self.executionMode = ActionsExecutionMode.ActionsRunInSequence
    self.currentActionIndex = 1
    self.finishedIndeces = {}
end

function ActionList:init(jsonData)
    if jsonData.executionMode ~= nil then
        self.executionMode = ActionsExecutionMode[jsonData.executionMode]
    end
    local size = #jsonData.actions
    for i=1,size do
        local jsonData2 = jsonData.actions[i]
        Cls = bt.getCls(jsonData2["$type"], jsonData2)
        local action = Cls.new()
        action:init(jsonData2)
        table.insert(self.actions, action)
    end
end

function ActionList:info()
    if #self.actions == 0 then
        return "No Actions"
    end
    local finalText = ""
    for k,action in pairs(self.actions) do
        if action ~= nil and action.isActive then
            finalText = finalText .. action.info() .. "\n"
        end
    end
    return finalText
end

function ActionList:onExecute()
    self.finishedIndeces = {}
    self.currentActionIndex = 1
end

function ActionList:onUpdate()
    if #self.actions == 0 then
        self:endAction(true)
        return 
    end
    if self.executionMode == ActionsExecutionMode.ActionsRunInParallel then
        for i,action in pairs(self.actions) do
            if not self:isContainInFinishedIndeces(i) then
                if not action.isActive then
                    table.insert( self.finishedIndeces, i)
                else
                    local status = action:executeAction(self.agent,self.blackboard)
                    if status == bt.Status.Failure then
                        self:endAction(false)
                        return
                    end
                    if status == bt.Status.Success then
                        table.insert( self.finishedIndeces, i)
                    end
                end
            end
        end
        if #self.finishedIndeces == #self.actions then
            self:endAction(true)
        end
    else
        for i = self.currentActionIndex,#self.actions do
            local action = self.actions[i]
            if action.isActive then
                local status = action:executeAction(self.agent,self.blackboard)
                if status == bt.Status.Failure then
                    self:endAction(false)
                    return
                end
                if status == bt.Status.Running then
                    self.currentActionIndex = i
                    return
                end
            end
        end
        self:endAction(true)
    end
end

function ActionList:isContainInFinishedIndeces(index)
    for k,v in pairs(self.finishedIndeces) do
        if v == index then
            return true
        end
    end
    return false
end

function ActionList:onStop()
    for k,action in pairs(self.actions) do
        action:endAction(nil)
    end
end

function ActionList:onPause()
    for k,action in pairs(self.actions) do
        action:pauseAction()
    end
end

function ActionList:destroy()
    self.finishedIndeces = nil
    for k,action in pairs(self.actions) do
        if action ~= nil then
            action:destroy()
            action = nil
        end
    end
end
